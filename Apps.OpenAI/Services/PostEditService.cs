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
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Models;
using Newtonsoft.Json;

namespace Apps.OpenAI.Services;

public class PostEditService(
    IXliffService xliffService,
    IGlossaryService glossaryService,
    IOpenAICompletionService openaiService,
    IFileManagementClient fileManagementClient)
{
    public async Task<PostEditResult> PostEditXliffAsync(PostEditInnerRequest request)
    {
        var result = new PostEditResult
        {
            ErrorMessages = [],
            Usage = new UsageDto()
        };

        try
        {
            var xliffDocument = await xliffService.LoadXliffDocumentAsync(request.Xliff);
            result.TotalSegmentsCount = xliffDocument.TranslationUnits.Count();

            var sourceLanguage = request.SourceLanguage ?? xliffDocument.SourceLanguage;
            var targetLanguage = request.TargetLanguage ?? xliffDocument.TargetLanguage;
            var unitsToProcess = FilterTranslationUnits(xliffDocument.TranslationUnits, request.PostEditLockedSegments ?? false);

            var modelMaxTokens = openaiService.GetModelMaxTokens(request.ModelId);
            var batches = xliffService.BatchTranslationUnits(unitsToProcess, request.BucketSize, modelMaxTokens);
            var batchOptions = new BatchProcessingOptions(
                request.ModelId,
                sourceLanguage,
                targetLanguage,
                request.Prompt,
                request.Glossary,
                request.FilterGlossary ?? true,
                request.BatchRetryAttempts ?? 3);

            var batchProcessingResult = await ProcessAllBatchesAsync(
                batches,
                batchOptions,
                request.NeverFail);

            result.ProcessedBatchesCount = batchProcessingResult.BatchesProcessed;
            result.Usage = SumUsageFromResults(batchProcessingResult.Usages);
            result.ErrorMessages.AddRange(batchProcessingResult.Errors);

            if (batchProcessingResult.Results.Any())
            {
                var tagOptions = new TagHandlingOptions(request.AddMissingTrailingTags ?? false);
                result.TargetsUpdatedCount = UpdateXliffWithResults(
                    xliffDocument,
                    batchProcessingResult.Results,
                    tagOptions);
            }

            var stream = xliffService.SerializeXliffDocument(xliffDocument);
            result.File = await fileManagementClient.UploadAsync(
                stream, request.Xliff.ContentType, request.Xliff.Name);

            return result;
        }
        catch (Exception ex) when (request.NeverFail)
        {
            result.ErrorMessages.Add($"Critical error: {ex.Message}");
            result.File = request.Xliff;
            return result;
        }
    }

    private IEnumerable<TranslationUnit> FilterTranslationUnits(IEnumerable<TranslationUnit> units, bool processLocked)
    {
        return processLocked ? units : units.Where(x => !x.IsLocked());
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
                        $"Failed to process batch {batchCounter}. Errors: {string.Join(", ", batchResult.ErrorMessages)}");
                }
            }
            catch (Exception ex) when (neverFail)
            {
                errors.Add($"Error in batch {batchCounter}: {ex.Message}");
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

    private async Task<PostEditBatchResult> ProcessBatchAsync(
        TranslationUnit[] batch,
        BatchProcessingOptions options)
    {
        var result = new PostEditBatchResult();

        try
        {
            // Get glossary prompt if needed
            string? glossaryPrompt = null;
            if (options.Glossary != null)
            {
                glossaryPrompt = await glossaryService.BuildGlossaryPromptAsync(
                    options.Glossary, batch, options.FilterGlossary);
            }

            // Build prompt and prepare messages
            var userPrompt = BuildUserPrompt(
                options.SourceLanguage,
                options.TargetLanguage,
                batch,
                options.Prompt,
                glossaryPrompt);

            var messages = new List<ChatMessageDto>
            {
                new(MessageRoles.System, PromptBuilder.DefaultSystemPrompt),
                new(MessageRoles.User, userPrompt)
            };

            // Call OpenAI and process response
            var completionResult = await CallOpenAIAndProcessResponseAsync(
                messages, options.ModelId, options.MaxRetryAttempts);

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

    private async Task<OpenAICompletionResult> CallOpenAIAndProcessResponseAsync(
            List<ChatMessageDto> messages,
            string modelId,
            int maxRetryAttempts)
    {
        var errors = new List<string>();
        var translations = new List<TranslationEntity>();
        var usage = new UsageDto();

        var chatCompletionResult = await openaiService.ExecuteChatCompletionWithRetryAsync(
            messages,
            modelId,
            new BaseChatRequest { Temperature = 0.1f },
            maxRetryAttempts);

        if (!chatCompletionResult.Success)
        {
            errors.Add(chatCompletionResult.Error ?? "Unknown error during OpenAI completion");
            return new OpenAICompletionResult(false, usage, errors, translations);
        }

        usage = chatCompletionResult.ChatCompletion.Usage;
        var choice = chatCompletionResult.ChatCompletion.Choices.First();
        var content = choice.Message.Content;

        if (choice.FinishReason == "length")
        {
            errors.Add("The response from OpenAI was truncated. Try reducing the batch size.");
            return new OpenAICompletionResult(false, usage, errors, translations);
        }

        try
        {
            var deserializedResponse = JsonConvert.DeserializeObject<TranslationEntities>(content);
            translations.AddRange(deserializedResponse.Translations);
            return new OpenAICompletionResult(true, usage, errors, translations);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to deserialize OpenAI response: {ex.Message}. Response: {content.Substring(0, Math.Min(content.Length, 200))}...");
            return new OpenAICompletionResult(false, usage, errors, translations);
        }
    }

    private string BuildUserPrompt(
        string sourceLanguage,
        string targetLanguage,
        TranslationUnit[] batch,
        string? additionalPrompt,
        string? glossaryPrompt)
    {
        var json = JsonConvert.SerializeObject(batch.Select(x => new { x.Id, x.Source, x.Target }));

        var prompt = $"Your input consists of sentences in {sourceLanguage} language with their translations into {targetLanguage}. " +
            "Review and edit the translated target text as necessary to ensure it is a correct and accurate translation of the source text. " +
            "If you see XML tags in the source also include them in the target text, don't delete or modify them. ";

        if (!string.IsNullOrEmpty(additionalPrompt))
        {
            prompt += additionalPrompt + " ";
        }

        if (!string.IsNullOrEmpty(glossaryPrompt))
        {
            prompt += glossaryPrompt + " ";
        }

        prompt += "Return only translation_id and target in your response. Sentences: \n" + json;
        return prompt;
    }

    private int UpdateXliffWithResults(
        XliffDocument document,
        List<TranslationEntity> updatedEntities,
        TagHandlingOptions tagOptions)
    {
        var translationDict = updatedEntities.ToDictionary(x => x.TranslationId, x => x.TranslatedText);
        var updatedTranslations = xliffService.CheckAndFixTagIssues(
            document.TranslationUnits, translationDict);

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