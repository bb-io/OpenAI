using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Services.Abstract;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Models;
using DocumentFormat.OpenXml;

namespace Apps.OpenAI.Services;

public class PostEditService(
    IXliffService xliffService,
    IGlossaryService glossaryService,
    IOpenAICompletionService openaiService,
    IResponseDeserializationService deserializationService,
    IPromptBuilderService promptBuilderService,
    IFileManagementClient fileManagementClient)
{
    public async Task<XliffResult> PostEditXliffAsync(OpenAiXliffInnerRequest request)
    {
        var result = new XliffResult
        {
            ErrorMessages = [],
            Usage = new UsageDto()
        };

        try
        {
            var xliffDocument = await xliffService.LoadXliffDocumentAsync(request.XliffFile);
            result.TotalSegmentsCount = xliffDocument.TranslationUnits.Count();

            var sourceLanguage = request.SourceLanguage ?? xliffDocument.SourceLanguage;
            var targetLanguage = request.TargetLanguage ?? xliffDocument.TargetLanguage;
            var unitsToProcess = FilterTranslationUnits(xliffDocument.TranslationUnits, request.PostEditLockedSegments ?? false, request.ProcessOnlyTargetState);
            result.LockedSegmentsExcludeCount = CountLockedSegments(xliffDocument.TranslationUnits, request.PostEditLockedSegments ?? false);

            var batches = xliffService.BatchTranslationUnits(unitsToProcess, request.BucketSize);
            var batchOptions = new BatchProcessingOptions(
                request.ModelId,
                sourceLanguage,
                targetLanguage,
                request.Prompt,
                request.Glossary,
                request.FilterGlossary ?? true,
                request.BatchRetryAttempts ?? 3,
                request.MaxTokens,
                null);

            var batchProcessingResult = await ProcessAllBatchesAsync(
                batches,
                batchOptions,
                request.NeverFail);

            result.ProcessedBatchesCount = batchProcessingResult.BatchesProcessed;
            result.Usage = SumUsageFromResults(batchProcessingResult.Usages);
            result.ErrorMessages.AddRange(batchProcessingResult.Errors);

            IdentifyDuplicateTranslationIdsAndLogErrors(batchProcessingResult);
            if (batchProcessingResult.Results.Any())
            {
                var tagOptions = new TagHandlingOptions(request.AddMissingTrailingTags ?? false);
                result.TargetsUpdatedCount = UpdateXliffWithResults(
                    xliffDocument,
                    batchProcessingResult.Results,
                    tagOptions,
                    request.DisableTagChecks);
            }

            var stream = xliffService.SerializeXliffDocument(xliffDocument);
            result.File = await fileManagementClient.UploadAsync(
                stream, request.XliffFile.ContentType, request.XliffFile.Name);

            return result;
        }
        catch (Exception ex) when (request.NeverFail)
        {
            result.ErrorMessages.Add($"Critical error: {ex.Message}");
            result.File = request.XliffFile;
            return result;
        }
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

    private IEnumerable<TranslationUnit> FilterTranslationUnits(IEnumerable<TranslationUnit> units, bool processLocked, string targetStateToFilter)
    {
        if (!string.IsNullOrEmpty(targetStateToFilter))
        { 
            units = units.Where(x => x.TargetAttributes.TryGetValue("state", out string value) && x.TargetAttributes["state"] == targetStateToFilter); 
        }

        return processLocked ? units : units.Where(x => !x.IsLocked());
    }

    private int CountLockedSegments(IEnumerable<TranslationUnit> units, bool processLocked)
    {
        if (processLocked)
        {
            return 0;
        }

        return units.Count(x => x.IsLocked());
    }

    private async Task<BatchProcessingResult> ProcessAllBatchesAsync(
            IEnumerable<IEnumerable<TranslationUnit>> batches,
            BatchProcessingOptions options,
            bool neverFail)
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
                    batch.ToArray(),
                    options);

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

    private UsageDto SumUsageFromResults(List<UsageDto> usages)
    {
        var usageDto = new UsageDto();
        foreach (var usage in usages)
        {
            usageDto += usage;
        }

        return usageDto;
    }

    private async Task<BatchResult> ProcessBatchAsync(
        TranslationUnit[] batch,
        BatchProcessingOptions options)
    {
        var result = new BatchResult();

        try
        {
            string? glossaryPrompt = null;
            if (options.Glossary != null)
            {
                glossaryPrompt = await glossaryService.BuildGlossaryPromptAsync(
                    options.Glossary, batch, options.FilterGlossary);
            }

            var userPrompt = promptBuilderService.BuildPostEditUserPrompt(
                options.SourceLanguage,
                options.TargetLanguage,
                batch,
                options.Prompt,
                glossaryPrompt);

            var messages = new List<ChatMessageDto>
            {
                new(MessageRoles.System, promptBuilderService.GetPostEditSystemPrompt()),
                new(MessageRoles.User, userPrompt)
            };

            var completionResult = await CallOpenAIAndProcessResponseAsync(
                messages, options.ModelId, options.MaxRetryAttempts, options.MaxTokens);

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

    private async Task<OpenAICompletionResult> CallOpenAIAndProcessResponseAsync(List<ChatMessageDto> messages, string modelId, int maxRetryAttempts, int? userMaxTokens = null)
    {
        var errors = new List<string>();
        var translations = new List<TranslationEntity>();
        var usage = new UsageDto();

        int currentAttempt = 0;
        bool success = false;

        while (!success && currentAttempt < maxRetryAttempts)
        {
            currentAttempt++;
            
            var chatCompletionResult = await openaiService.ExecuteChatCompletionAsync(
                messages,
                modelId,
                new BaseChatRequest { Temperature = modelId.Contains("gpt") ? 0.1f : 1f, MaximumTokens = userMaxTokens },
                ResponseFormats.GetXliffResponseFormat());

            if (!chatCompletionResult.Success)
            {
                var errorMessage = $"Attempt {currentAttempt}/{maxRetryAttempts}: API call failed - {chatCompletionResult.Error ?? "Unknown error during OpenAI completion"}";
                errors.Add(errorMessage);
                continue;
            }

            usage = chatCompletionResult.ChatCompletion.Usage;
            var choice = chatCompletionResult.ChatCompletion.Choices.First();
            var content = choice.Message.Content;

            if (choice.FinishReason == "length")
            {
                errors.Add($"Attempt {currentAttempt}/{maxRetryAttempts}: The response from OpenAI was truncated. Try reducing the batch size.");
            }

            var deserializationResult = deserializationService.DeserializeResponse(content);
            if (deserializationResult.Success)
            {
                success = true;
                translations.AddRange(deserializationResult.Translations);
            }
            else
            {
                errors.Add($"Attempt {currentAttempt}/{maxRetryAttempts}: {deserializationResult.Error}");
            }
        }

        return new OpenAICompletionResult(success, usage, errors, translations);
    }

    private int UpdateXliffWithResults(
        XliffDocument document,
        List<TranslationEntity> updatedEntities,
        TagHandlingOptions tagOptions,
        bool disableTagChecks)
    {
        var translationDict = updatedEntities.ToDictionary(x => x.TranslationId, x => x.TranslatedText);
        var updatedTranslations = xliffService.CheckAndFixTagIssues(
            document.TranslationUnits, translationDict, disableTagChecks);

        return UpdateXliffDocument(document, updatedTranslations, tagOptions.AddMissingTrailingTags);
    }

    private int UpdateXliffDocument(
        XliffDocument document,
        Dictionary<string, string> updatedTranslations,
        bool addMissingTrailingTags)
    {
        int updatedCount = 0;
        foreach (var (translationId, translatedText) in updatedTranslations)
        {
            var translationUnit = document.TranslationUnits.FirstOrDefault(tu => tu.Id == translationId);
            if (translationUnit == null || translationUnit.IsLocked() || string.IsNullOrEmpty(translatedText))
            {
                continue;
            }

            if (translationUnit.Target != translatedText)
            {
                updatedCount++;
                translationUnit.Target = addMissingTrailingTags
                    ? ApplyTagsIfNeeded(translationUnit.Source, translatedText)
                    : translatedText;
            }
        }

        return updatedCount;
    }

    private string ApplyTagsIfNeeded(string sourceContent, string translatedText)
    {
        var tagPattern = @"<(?<tag>\w+)(?<attributes>[^>]*)>(?<content>.*?)</\k<tag>>";
        var sourceMatch = Regex.Match(sourceContent, tagPattern, RegexOptions.Singleline);
        if (!sourceMatch.Success)
        {
            return translatedText;
        }

        var tagName = sourceMatch.Groups["tag"].Value;
        var tagAttributes = sourceMatch.Groups["attributes"].Value;
        var openingTag = $"<{tagName}{tagAttributes}>";
        var closingTag = $"</{tagName}>";

        if (!translatedText.Contains(openingTag) && !translatedText.Contains(closingTag))
        {
            return openingTag + translatedText + closingTag;
        }

        return translatedText;
    }
}