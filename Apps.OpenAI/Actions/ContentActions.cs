extern alias XliffContent;

using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Models.Content;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XliffContent::Blackbird.Xliff.Utils.Models.Content;
using XliffContent::Blackbird.Xliff.Utils.Serializers.Html;
using XliffContent::Blackbird.Xliff.Utils.Serializers.Xliff2;
using XliffContent::Blackbird.Xliff.Utils.Constants;
using Blackbird.Xliff.Utils.Models;
using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Services;
using DocumentFormat.OpenXml.Office2016.Excel;
using Blackbird.Xliff.Utils;
using Apps.OpenAI.Services.Abstract;
using Apps.OpenAI.Utils;
using System.IO;
using XliffContent::Blackbird.Xliff.Utils.Extensions;

namespace Apps.OpenAI.Actions;

[ActionList]
public class ContentActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Translate",
        Description = "[Experimental] Translate a content file. Currently supports HTML and XLIFF as input.")]
    public async Task<ContentProcessingResult> TranslateContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] TranslateContentRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be translated at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = 1500)
    {

        var result = new ContentProcessingResult();
        var stream = await fileManagementClient.DownloadAsync(input.File);
        var content = await FileGroup.TryParse(stream);
        content.SourceLanguage ??= input.SourceLanguage;
        content.TargetLanguage ??= input.TargetLanguage;
        if (content.SourceLanguage == null) throw new PluginMisconfigurationException("The source language is not defined yet. Please assign the source language in this action.");
        if (content.TargetLanguage == null) throw new PluginMisconfigurationException("The target language is not defined yet. Please assign the target language in this action.");

        var segments = content.IterateSegments();
        result.TotalSegmentsCount = segments.Count();
        segments = segments.Where(x => !x.Ignorable && x.IsInitial());
        result.TotalTranslatable = segments.Count();

        var idSegments = segments.Select((x, i) => new { Id = i + 1, Value = x }).ToDictionary(x => x.Id.ToString(), x => x.Value);
        var batches = BatchWithIds(idSegments, bucketSize ?? 1500);

        var batchOptions = new BatchProcessingOptions(
                modelIdentifier.GetModel(),
                content.SourceLanguage,
                content.TargetLanguage,
                prompt,
                glossary.Glossary,
                true,
                3,
                null);

        var batchProcessingResult = await ProcessAllBatchesAsync(batches, batchOptions, false, false);

        result.ProcessedBatchesCount = batchProcessingResult.BatchesProcessed;
        result.Usage = SumUsageFromResults(batchProcessingResult.Usages);

        IdentifyDuplicateTranslationIdsAndLogErrors(batchProcessingResult);

        var updatedCount = 0;
        foreach (var batchResult in batchProcessingResult.Results)
        {
            Segment segment = null;
            idSegments.TryGetValue(batchResult.TranslationId, out segment);
            if (segment == null) continue;

            var shouldTranslateFromState = segment.State == null || segment.State == SegmentState.Initial;
            if ( !shouldTranslateFromState || string.IsNullOrEmpty(batchResult.TranslatedText))
            {
                continue;
            }

            if (segment.GetTarget() != batchResult.TranslatedText)
            {
                updatedCount++;
                segment.SetTarget(batchResult.TranslatedText, TagParsing.Html); // Update tagparsing setting for other content types
                segment.State = SegmentState.Translated;
            }
        }

        result.TargetsUpdatedCount = updatedCount;

        var streamResult = Xliff2Serializer.Serialize(content).ToStream();
        var fileName = input.File.Name.EndsWith("xliff") || input.File.Name.EndsWith("xlf") ? input.File.Name : input.File.Name + ".xliff";
        result.Content = await fileManagementClient.UploadAsync(streamResult, "application/xliff+xml", fileName);

        return result;
    }

    [Action("Edit",
    Description = "[Experimental] Edit a content file. Only supports XLIFF input (received from any action that returns bilingual).")]
    public async Task<ContentProcessingEditResult> EditContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
    [ActionParameter] EditContentRequest input,
    [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
    [ActionParameter] GlossaryRequest glossary,
    [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be translated at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = 1500)
    {

        var result = new ContentProcessingEditResult();
        var stream = await fileManagementClient.DownloadAsync(input.File);
        var content = await FileGroup.TryParse(stream);

        var segments = content.IterateSegments();
        result.TotalSegmentsCount = segments.Count();
        segments = segments.Where(x => !x.Ignorable && x.State == SegmentState.Translated);
        result.TotalEditable = segments.Count();

        var idSegments = segments.Select((x, i) => new { Id = i + 1, Value = x }).ToDictionary(x => x.Id.ToString(), x => x.Value);
        var batches = BatchWithIds(idSegments, bucketSize ?? 1500);

        var batchOptions = new BatchProcessingOptions(
                modelIdentifier.GetModel(),
                content.SourceLanguage,
                content.TargetLanguage,
                prompt,
                glossary.Glossary,
                true,
                3,
                null);

        var batchProcessingResult = await ProcessAllBatchesAsync(batches, batchOptions, false, true);

        result.ProcessedBatchesCount = batchProcessingResult.BatchesProcessed;
        result.Usage = SumUsageFromResults(batchProcessingResult.Usages);

        IdentifyDuplicateTranslationIdsAndLogErrors(batchProcessingResult);

        var updatedCount = 0;
        foreach (var batchResult in batchProcessingResult.Results)
        {
            Segment segment = null;
            idSegments.TryGetValue(batchResult.TranslationId, out segment);
            if (segment == null) continue;

            if (segment.State != SegmentState.Translated || string.IsNullOrEmpty(batchResult.TranslatedText))
            {
                continue;
            }

            if (segment.GetTarget() != batchResult.TranslatedText)
            {
                updatedCount++;
                segment.SetTarget(batchResult.TranslatedText, TagParsing.Html); // Update tagparsing setting for other content types
                segment.State = SegmentState.Reviewed;
            }
        }

        result.TargetsUpdatedCount = updatedCount;

        var streamResult = Xliff2Serializer.Serialize(content).ToStream();
        var fileName = input.File.Name.EndsWith("xliff") || input.File.Name.EndsWith("xlf") ? input.File.Name : input.File.Name + ".xliff";
        result.Content = await fileManagementClient.UploadAsync(streamResult, "application/xliff+xml", fileName);

        return result;
    }

    private void IdentifyDuplicateTranslationIdsAndLogErrors(BatchProcessingResult result)
    {
        var duplicates = result.Results.GroupBy(x => x.TranslationId)
            .Where(g => g.Count() > 1)
            .Select(g => new { TranslationId = g.Key, Count = g.Count() })
            .ToList();

        if (duplicates.Any())
        {
            foreach (var duplicate in duplicates)
            {
                result.Errors.Add($"Duplicate translation ID found: {duplicate.TranslationId} appears {duplicate.Count} times");
            }
        }
    }

    private UsageDto SumUsageFromResults(List<UsageDto> usages)
    {
        var usageDto = new UsageDto();
        foreach (var usage in usages)
        {
            usageDto += usage;
        }

        return usageDto;
    }

    private async Task<BatchProcessingResult> ProcessAllBatchesAsync(
            IEnumerable<Dictionary<string, Segment>> batches,
            BatchProcessingOptions options,
            bool neverFail,
            bool postEdit)
    {
        var allResults = new List<TranslationEntity>();
        var errors = new List<string>();
        var usages = new List<UsageDto>();
        int batchCounter = 0;

        foreach (var batch in batches)
        {
            batchCounter++;
            var batchSize = batch.Count();

            try
            {
                var batchResult = await ProcessBatchAsync(
                    batch,
                    options,
                    postEdit);

                if (batchResult.IsSuccess)
                {
                    allResults.AddRange(batchResult.UpdatedTranslations);
                }

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
        }

        return new BatchProcessingResult(batchCounter, allResults, usages, errors);
    }

    private async Task<BatchResult> ProcessBatchAsync(
    Dictionary<string, Segment> batch,
    BatchProcessingOptions options,
    bool postEdit)
    {
        var result = new BatchResult();
        var glossaryService = new ContentGlossaryService(FileManagementClient);
        var promptBuilderService = new ContentPromptBuilderService();

        try
        {
            string? glossaryPrompt = null;
            if (options.Glossary != null)
            {
                glossaryPrompt = await glossaryService.BuildGlossaryPromptAsync(
                    options.Glossary, batch.Select(x => x.Value), options.FilterGlossary);
            }

            var userPrompt = postEdit ? promptBuilderService.BuildPostEditUserPrompt(
                options.SourceLanguage,
                options.TargetLanguage,
                batch,
                options.Prompt,
                glossaryPrompt
                ) : promptBuilderService.BuildProcessUserPrompt(
                options.SourceLanguage,
                options.TargetLanguage,
                batch,
                options.Prompt,
                glossaryPrompt);

            var messages = new List<ChatMessageDto>
            {
                new(MessageRoles.System, postEdit ? promptBuilderService.GetPostEditSystemPrompt() : promptBuilderService.GetProcessSystemPrompt()),
                new(MessageRoles.User, userPrompt)
            };

            var completionResult = await CallOpenAIAndProcessResponseAsync(
                messages, options);

            result.IsSuccess = completionResult.IsSuccess;
            result.Usage = completionResult.Usage;
            result.ErrorMessages.AddRange(completionResult.Errors);
            result.UpdatedTranslations.AddRange(completionResult.Translations);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessages.Add($"Unexpected error processing batch: {ex.Message}");
            return result;
        }
    }

    private async Task<OpenAICompletionResult> CallOpenAIAndProcessResponseAsync(List<ChatMessageDto> messages, BatchProcessingOptions options)
    {
        var errors = new List<string>();
        var translations = new List<TranslationEntity>();
        var usage = new UsageDto();
        var openaiService = new OpenAICompletionService(Client);
        var deserializationService = new ResponseDeserializationService();

        int currentAttempt = 0;
        bool success = false;

        while (!success && currentAttempt < options.MaxRetryAttempts)
        {
            currentAttempt++;

            var chatCompletionResult = await openaiService.ExecuteChatCompletionAsync(
                messages,
                options.ModelId,
                new BaseChatRequest { MaximumTokens = options.MaxTokens },
                ResponseFormats.GetXliffResponseFormat());

            if (!chatCompletionResult.Success)
            {
                var errorMessage = $"Attempt {currentAttempt}/{options.MaxRetryAttempts}: API call failed - {chatCompletionResult.Error ?? "Unknown error during OpenAI completion"}";
                errors.Add(errorMessage);
                continue;
            }

            usage = chatCompletionResult.ChatCompletion.Usage;
            var choice = chatCompletionResult.ChatCompletion.Choices.First();
            var content = choice.Message.Content;

            if (choice.FinishReason == "length")
            {
                errors.Add($"Attempt {currentAttempt}/{options.MaxRetryAttempts}: The response from OpenAI was truncated. Try reducing the batch size.");
            }

            var deserializationResult = deserializationService.DeserializeResponse(content);
            if (deserializationResult.Success)
            {
                success = true;
                translations.AddRange(deserializationResult.Translations);
            }
            else
            {
                errors.Add($"Attempt {currentAttempt}/{options.MaxRetryAttempts}: {deserializationResult.Error}");
            }
        }

        return new OpenAICompletionResult(success, usage, errors, translations);
    }

    private static IEnumerable<Dictionary<string, Segment>> BatchWithIds(Dictionary<string, Segment> segments, int batchSize = 1500)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        }

        return segments
            .Select((unit, index) => new { Segment = unit, Index = index })
            .GroupBy(item => item.Index / batchSize)
            .Select(group => group.Select(item => item.Segment ).ToDictionary());
    }
}
