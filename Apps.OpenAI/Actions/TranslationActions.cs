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

        var result = await AiAdapters.Mapped(() => processor.TranslateAsync(TranslateOptions(input, file, glossaryFile, prompt)));

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

    [Action("Translate in background", Description = "Starts background translation of a file and outputs a batch ID and a transformation file to download the result later.")]
    public async Task<BackgroundProcessingResponse> TranslateInBackground([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] TranslateContentRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] ReasoningEffortRequest reasoningEffortRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the maximum number of source texts to be translated in one request of the batch. Default value: 250. Buckets are also sized to fit the model's output limit. (See our documentation for an explanation)")] int? bucketSize = null)
    {
        var effectiveModel = await ResolveTextChatModelAsync(modelIdentifier.ModelId);
        var settings = new OpenAiLlmSettings(reasoningEffortRequest.ReasoningEffort);
        var client = CreateLlmClient(effectiveModel, settings);
        var processor = CreateBackgroundProcessor(CreateBatchClient(effectiveModel, settings), client, effectiveModel, bucketSize);

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(glossary.Glossary);

        var started = await AiAdapters.Mapped(() =>
            processor.StartTranslateAsync(TranslateOptions(input, file, glossaryFile, prompt)));

        return await ToBackgroundResponseAsync(started, client, effectiveModel);
    }

    private static TranslateStreamOptions TranslateOptions(TranslateContentRequest input, Stream file, Stream? glossaryFile, string? prompt) => new()
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
    };

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
