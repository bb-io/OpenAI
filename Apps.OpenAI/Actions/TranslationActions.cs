using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Content;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Apps.OpenAI.Models.Responses.Background;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Services;
using Apps.OpenAI.Utils;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Summary.Models;
using Blackbird.Ai.Text.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Extensions;

namespace Apps.OpenAI.Actions;

[ActionList("Translation")]
public class TranslationActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [BlueprintActionDefinition(BlueprintAction.TranslateFile)]
    [Action("Translate", Description = "Translates file content from a CMS or file storage and outputs localized content for compatible actions.")]
    public async Task<ContentProcessingResult> TranslateContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] TranslateContentRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] ReasoningEffortRequest reasoningEffortRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the maximum number of source texts to be translated at once. Default value: 250. Buckets are also sized to fit the model's output limit. (See our documentation for an explanation)")] int? bucketSize = null)
    {
        var effectiveModel = await ResolveTextChatModelAsync(modelIdentifier.ModelId);
        var client = CreateLlmClient(effectiveModel, new OpenAiLlmSettings(reasoningEffortRequest.ReasoningEffort));
        var processor = CreateContentProcessor(client, effectiveModel, bucketSize);

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(glossary.Glossary);

        var result = await AiAdapters.Mapped(() => processor.TranslateAsync(new TranslateStreamOptions
        {
            File = file,
            FileName = input.File.Name,
            MediaType = input.File.ContentType,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            OutputFileHandling = input.OutputFileHandling,
            GlossaryTbx = glossaryFile,
            AdditionalInstructions = prompt,
            Summary = SummaryOptions.For(input.DocumentSummary)
        }));

        var content = result.Content;

        return new ContentProcessingResult
        {
            File = await UploadAsync(result.File),
            Usage = ToUsageDto(content.Usage, client, effectiveModel),
            TotalSegmentsCount = content.TotalSegments,
            TotalWordsCount = content.TotalWords,
            TotalTranslatable = content.ProcessableSegments,
            TotalWordsTranslatable = content.ProcessableWords,
            TargetsUpdatedCount = content.UpdatedSegments,
            TotalWordsUpdated = content.UpdatedWords,
            ProcessedBatchesCount = content.BucketsProcessed,
            SystemPrompt = content.SystemPrompt ?? string.Empty
        };
    }

    [Action("Translate in background", Description = "Starts background translation for a file and outputs a batch ID to download results later.")]
    public async Task<BackgroundProcessingResponse> TranslateInBackground([ActionParameter] StartBackgroundProcessRequest startBackgroundProcessRequest)
    {
        var effectiveModel = await ResolveTextChatModelAsync(startBackgroundProcessRequest.ModelId);
        await using var downloadedStream = await FileManagementClient.DownloadAsync(startBackgroundProcessRequest.File);
        // await using var stream = downloadedStream;
        await using var stream = new MemoryStream();
        await downloadedStream.CopyToAsync(stream);
        stream.Position = 0;
        var loadResult = Transformation.Load(stream, startBackgroundProcessRequest.File.Name, startBackgroundProcessRequest.File.ContentType);
        if (!loadResult.Success)
            throw new PluginMisconfigurationException(loadResult.Error);

        var content = loadResult.Value;

        content.SourceLanguage ??= startBackgroundProcessRequest.SourceLanguage;
        content.TargetLanguage ??= startBackgroundProcessRequest.TargetLanguage;

        if (content.TargetLanguage == null)
            throw new PluginMisconfigurationException("The target language is not defined yet. Please assign the target language in this action.");

        if (content.SourceLanguage == null)
        {
            var sourceContentResult = content.Source();
            if (!sourceContentResult.Success)
                throw new PluginMisconfigurationException(sourceContentResult.Error);
            var sourceContent = sourceContentResult.Value;
            content.SourceLanguage = await IdentifySourceLanguage(effectiveModel, sourceContent.GetPlaintext());
        }

        var units = content.GetUnits();
        var segments = units.SelectMany(x => x.Segments);
        segments = segments.GetSegmentsForTranslation().ToList();

        var batchRequests = new List<object>();

        Glossary? blackbirdGlossary = await ProcessGlossaryFromFile(startBackgroundProcessRequest.Glossary);
        Dictionary<string, List<GlossaryEntry>>? glossaryLookup = null;
        if (blackbirdGlossary != null)
        {
            glossaryLookup = CreateGlossaryLookup(blackbirdGlossary);
        }

        var systemPromptBase = $"Translate the following texts from {content.SourceLanguage} to {content.TargetLanguage}. " +
                            "Preserve the original format, tags, and structure. Return the translations in the specified JSON format.";

        if (startBackgroundProcessRequest.AdditionalInstructions != null)
        {
            systemPromptBase += $" Additional instructions: {startBackgroundProcessRequest.AdditionalInstructions}.";
        }

        if (glossaryLookup != null)
        {
            systemPromptBase += " Use the provided glossary to ensure accurate translations of specific terms.";
        }

        var bucketSize = startBackgroundProcessRequest.GetBucketingSize();
        var segmentList = segments.ToList();

        // Create buckets by splitting segments into chunks
        var segmentBuckets = new List<List<Segment>>();
        for (int i = 0; i < segmentList.Count; i += bucketSize)
        {
            var bucket = segmentList.Skip(i).Take(bucketSize).ToList();
            segmentBuckets.Add(bucket);
        }

        foreach (var (bucket, bucketIndex) in segmentBuckets.Select((bucket, index) => (bucket, index)))
        {
            var segmentTexts = new List<string>();
            var segmentIds = new List<string>();

            foreach (var (segment, segmentIndex) in bucket.Select((seg, idx) => (seg, idx)))
            {
                var globalIndex = bucketIndex * bucketSize + segmentIndex;
                var sourceText = segment.GetSource();
                segmentTexts.Add(sourceText);
                segmentIds.Add(globalIndex.ToString());
            }

            var userPrompt = "Translate the following texts:\n\n";
            for (int i = 0; i < segmentTexts.Count; i++)
            {
                userPrompt += $"ID: {segmentIds[i]}\nText: {segmentTexts[i]}\n\n";
            }

            if (glossaryLookup != null)
            {
                var combinedText = string.Join(" ", segmentTexts);
                var glossaryPromptPart = GetOptimizedGlossaryPromptPart(glossaryLookup, combinedText);
                if (!string.IsNullOrEmpty(glossaryPromptPart))
                {
                    userPrompt += $"\nGlossary terms:\n{glossaryPromptPart}";
                }
            }

            var batchRequest = new
            {
                custom_id = bucketIndex.ToString(),
                method = "POST",
                url = "/v1/responses",
                body = new
                {
                    model = effectiveModel,
                    store = false,
                    input = new object[]
                    {
                        new
                        {
                            role = "system",
                            content = systemPromptBase
                        },
                        new
                        {
                            role = "user",
                            content = userPrompt
                        }
                    },
                    text = new
                    {
                        format = ResponseFormats.GetXliffResponseFormat()
                    }
                }
            };

            batchRequests.Add(batchRequest);
        }

        var batchResponse = await CreateBatchAsync(batchRequests);
        content.MetaData.Set([Meta.Categories.Blackbird], "background-type", "translate");
        return new BackgroundProcessingResponse
        {
            BatchId = batchResponse.Id,
            Status = batchResponse.Status,
            CreatedAt = batchResponse.CreatedAt,
            ExpectedCompletionTime = batchResponse.ExpectedCompletionTime,
            TransformationFile = await FileManagementClient.UploadAsync(content.ToStream(), MediaTypes.Xliff2, content.BilingualFileName)
        };
    }

    [BlueprintActionDefinition(BlueprintAction.TranslateText)]
    [Action("Translate text", Description = "Outputs localized text for the provided input text.")]
    public async Task<TranslateTextResponse> LocalizeText([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] LocalizeTextRequest input,
        [ActionParameter] GlossaryRequest glossary)
    {
        var client = CreateLlmClient(modelIdentifier.ModelId, new OpenAiLlmSettings(
            input.ReasoningEffort, input.MaximumTokens, input.Temperature, input.TopP));
        var processor = CreateTextProcessor(client, modelIdentifier.ModelId);

        await using var glossaryFile = await DownloadOptionalAsync(glossary.Glossary);

        var result = await AiAdapters.Mapped(() => processor.TranslateAsync(new TranslateTextOptions
        {
            Text = input.Text,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            GlossaryTbx = glossaryFile
        }));

        if (result.Content.UnresolvedSegments > 0)
            throw new PluginApplicationException(string.Join(Environment.NewLine, result.Content.Warnings));

        return new()
        {
            SystemPrompt = result.Content.SystemPrompt ?? string.Empty,
            UserPrompt = LastUserPrompt(result.Content),
            TranslatedText = result.Text,
            Usage = ToUsageDto(result.Content.Usage, client, modelIdentifier.ModelId)
        };
    }

}
