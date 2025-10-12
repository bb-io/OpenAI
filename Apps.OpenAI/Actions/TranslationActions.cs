using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Models.Content;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Services;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Constants;
using Blackbird.Applications.SDK.Blueprints;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Responses.Background;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using Blackbird.Filters.Xliff.Xliff1;

namespace Apps.OpenAI.Actions;

[ActionList("Translation")]
public class TranslationActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseActions(invocationContext, fileManagementClient)
{
    [BlueprintActionDefinition(BlueprintAction.TranslateFile)]
    [Action("Translate", Description = "Translate file content retrieved from a CMS or file storage. The output can be used in compatible actions.")]
    public async Task<ContentProcessingResult> TranslateContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] TranslateContentRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] ReasoningEffortRequest reasoningEffortRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be translated at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = null)
    {
        var neverFail = false;
        var batchSize = bucketSize ?? 1500;
        var result = new ContentProcessingResult();
        var stream = await fileManagementClient.DownloadAsync(input.File);
        var content = await Transformation.Parse(stream, input.File.Name);
        content.SourceLanguage ??= input.SourceLanguage;
        content.TargetLanguage ??= input.TargetLanguage;        
        if (content.TargetLanguage == null) throw new PluginMisconfigurationException("The target language is not defined yet. Please assign the target language in this action.");

        if (content.SourceLanguage == null)
        {
            content.SourceLanguage = await IdentifySourceLanguage(modelIdentifier, content.Source().GetPlaintext());
        }

        var batchProcessingService = new BatchProcessingService(Client, FileManagementClient);
        var batchOptions = new BatchProcessingOptions(
            modelIdentifier.GetModel(),
            content.SourceLanguage,
            content.TargetLanguage,
            prompt,
            glossary.Glossary,
            true,
            3,
            null,
            reasoningEffortRequest.ReasoningEffort,
            content.Notes);

        var errors = new List<string>();
        var usages = new List<UsageDto>();
        int batchCounter = 0;
        var systemprompt = string.Empty;

        async Task<IEnumerable<TranslationEntity>> BatchTranslate(IEnumerable<(Unit Unit, Segment Segment)> batch)
        {
            var idSegments = batch.Select((x, i) => new { Id = i + 1, Value = x }).ToDictionary(x => x.Id.ToString(), x => x.Value.Segment);
            var allResults = new List<TranslationEntity>();
            batchCounter++;
            try
            {
                var batchResult = await batchProcessingService.ProcessBatchAsync(idSegments, batchOptions, false);
                if (batchResult.IsSuccess)
                {
                    allResults.AddRange(batchResult.UpdatedTranslations);
                }

                systemprompt = batchResult.SystemPrompt;

                var duplicates = batchResult.UpdatedTranslations.GroupBy(x => x.TranslationId)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { TranslationId = g.Key, Count = g.Count() })
                    .ToList();

                errors.AddRange(duplicates.Select(duplicate => $"Duplicate translation ID found: {duplicate.TranslationId} appears {duplicate.Count} times"));
                errors.AddRange(batchResult.ErrorMessages);
                usages.Add(batchResult.Usage);

                if (!batchResult.IsSuccess && !neverFail)
                {
                    throw new PluginApplicationException(
                        $"Failed to process batch {batchCounter} (size: {batchSize}). Errors: {string.Join(", ", batchResult.ErrorMessages)}");
                }
            }
            catch (Exception ex) when (neverFail)
            {
                errors.Add($"Error in batch {batchCounter} (size: {batchSize}): {ex.Message}");
            }
            
            return allResults;
        }

        var units = content.GetUnits();        
        result.TotalSegmentsCount = units.SelectMany(x => x.Segments).Count();
        units = units.Where(x => x.IsInitial);
        var segments = units.Where(x => x.IsInitial).SelectMany(x => x.Segments);
        result.TotalTranslatable = segments.Count();

        var processedBatches = await units.Where(x => x.IsInitial).Batch(batchSize).Process(BatchTranslate);
        result.ProcessedBatchesCount = batchCounter;
        result.Usage = UsageDto.Sum(usages);
        result.SystemPrompt = systemprompt;

        var updatedCount = 0;
        foreach (var (unit, results) in processedBatches)
        {
            foreach(var (segment, translation) in results) 
            {
                var shouldTranslateFromState = segment.State == null || segment.State == SegmentState.Initial;
                if (!shouldTranslateFromState || string.IsNullOrEmpty(translation.TranslatedText))
                {
                    continue;
                }

                if (segment.GetTarget() != translation.TranslatedText)
                {
                    updatedCount++;
                    segment.SetTarget(translation.TranslatedText);
                    segment.State = SegmentState.Translated;
                }
            }

            unit.Provenance.Translation.Tool = modelIdentifier.GetModel();
            unit.Provenance.Translation.ToolReference = $"https://openai.com/{modelIdentifier.GetModel()}";
        }

        result.TargetsUpdatedCount = updatedCount;

        if (input.OutputFileHandling == "original")
        {
            var targetContent = content.Target();
            result.File = await fileManagementClient.UploadAsync(targetContent.Serialize().ToStream(), targetContent.OriginalMediaType, targetContent.OriginalName);
        } 
        else if (input.OutputFileHandling == "xliff1")
        {
            var xliff1String = Xliff1Serializer.Serialize(content);
            result.File = await fileManagementClient.UploadAsync(xliff1String.ToStream(), MediaTypes.Xliff, content.XliffFileName);
        }
        else
        {
            result.File = await fileManagementClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff, content.XliffFileName);
        }       

        return result;
    }    

    [Action("Translate in background", Description = "Start background translation process for a file. This action will return a batch ID that can be used to download the results later.")]
    public async Task<BackgroundProcessingResponse> TranslateInBackground([ActionParameter] StartBackgroundProcessRequest startBackgroundProcessRequest)
    {
        var stream = await fileManagementClient.DownloadAsync(startBackgroundProcessRequest.File);
        var content = await Transformation.Parse(stream, startBackgroundProcessRequest.File.Name);
        
        content.SourceLanguage ??= startBackgroundProcessRequest.SourceLanguage;
        content.TargetLanguage ??= startBackgroundProcessRequest.TargetLanguage;
        
        if (content.TargetLanguage == null) 
            throw new PluginMisconfigurationException("The target language is not defined yet. Please assign the target language in this action.");

        if (content.SourceLanguage == null)
        {
            content.SourceLanguage = await IdentifySourceLanguage(startBackgroundProcessRequest, content.Source().GetPlaintext());
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
        
        if(glossaryLookup != null)
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
                url = "/v1/chat/completions",
                body = new
                {
                    model = startBackgroundProcessRequest.GetModel(),
                    messages = new object[]
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
                    response_format = ResponseFormats.GetXliffResponseFormat()
                }
            };

            batchRequests.Add(batchRequest);
        }

        var batchResponse = await CreateBatchAsync(batchRequests);
        content.MetaData.Add(new Metadata("background-type", "translate") { Category = [Meta.Categories.Blackbird]});
        return new BackgroundProcessingResponse
        {
            BatchId = batchResponse.Id,
            Status = batchResponse.Status,
            CreatedAt = batchResponse.CreatedAt,
            ExpectedCompletionTime = batchResponse.ExpectedCompletionTime,
            TransformationFile = await fileManagementClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff, content.XliffFileName)
        };
    }

    [BlueprintActionDefinition(BlueprintAction.TranslateText)]
    [Action("Translate text", Description = "Localize the text provided.")]
    public async Task<TranslateTextResponse> LocalizeText([ActionParameter] TextChatModelIdentifier modelIdentifier, 
        [ActionParameter] LocalizeTextRequest input, 
        [ActionParameter] GlossaryRequest glossary)
    {
        var systemPrompt = "You are a text localizer. Localize the provided text for the specified locale while " +
                           "preserving the original text structure. Respond with localized text.";

        var userPrompt = @$"
                    Original text: {input.Text}
                    Locale: {input.TargetLanguage} 
                
                    ";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.Text, true);
            if (glossaryPromptPart != null)
            {
                userPrompt +=
                    "\nEnhance the localized text by incorporating relevant terms from our glossary where applicable. " +
                    "If you encounter terms from the glossary in the text, ensure that the localized text aligns " +
                    "with the glossary entries for the respective languages. If a term has variations or synonyms, " +
                    "consider them and choose the most appropriate translation from the glossary to maintain " +
                    $"consistency and precision. {glossaryPromptPart}";
            }
        }

        userPrompt += "Localized text: ";

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteChatCompletion(messages, modelIdentifier.GetModel(), input);

        return new()
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            TranslatedText = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

}
