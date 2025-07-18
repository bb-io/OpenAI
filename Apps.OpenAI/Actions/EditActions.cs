﻿using Apps.OpenAI.Actions.Base;
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
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Utils;
using Apps.OpenAI.Constants;

namespace Apps.OpenAI.Actions;

[ActionList("Editing")]
public class EditActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseActions(invocationContext, fileManagementClient)
{

    [BlueprintActionDefinition(BlueprintAction.EditFile)]
    [Action("Edit", Description = "Edit a translation. This action assumes you have previously translated content in Blackbird through any translation action.")]
    public async Task<ContentProcessingEditResult> EditContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] EditContentRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be edited at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = null)
    {
        var neverFail = false;
        var batchSize = bucketSize ?? 1500;
        var result = new ContentProcessingEditResult();
        var stream = await fileManagementClient.DownloadAsync(input.File);

        var content = await ErrorHandler.ExecuteWithErrorHandlingAsync(()=>Transformation.Parse(stream, input.File.Name));

        var batchProcessingService = new BatchProcessingService(Client, FileManagementClient);
        var batchOptions = new BatchProcessingOptions(
            modelIdentifier.GetModel(),
            content.SourceLanguage,
            content.TargetLanguage,
            prompt,
            glossary.Glossary,
            true,
            3,
            null);

        var errors = new List<string>();
        var usages = new List<UsageDto>();
        int batchCounter = 0;

        async Task<IEnumerable<TranslationEntity>> BatchTranslate(IEnumerable<Segment> batch)
        {
            var idSegments = batch.Select((x, i) => new { Id = i + 1, Value = x }).ToDictionary(x => x.Id.ToString(), x => x.Value);
            var allResults = new List<TranslationEntity>();
            batchCounter++;
            try
            {
                var batchResult = await batchProcessingService.ProcessBatchAsync(idSegments, batchOptions, true);
                if (batchResult.IsSuccess)
                {
                    allResults.AddRange(batchResult.UpdatedTranslations);
                }

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

        var segments = content.GetSegments();
        result.TotalSegmentsCount = segments.Count();
        segments = segments.Where(x => !x.IsIgnorbale && x.State == SegmentState.Translated);
        result.TotalSegmentsReviewed = segments.Count();

        var processedBatches = await segments.Batch(batchSize).Process(BatchTranslate);
        result.ProcessedBatchesCount = batchCounter;
        result.Usage = UsageDto.Sum(usages);

        var updatedCount = 0;
        foreach (var (segment, translation) in processedBatches)
        {
            if (segment.GetTarget() != translation.TranslatedText)
            {
                updatedCount++;
                segment.SetTarget(translation.TranslatedText);
            }
            segment.State = SegmentState.Reviewed;
        }

        result.TotalSegmentsUpdated = updatedCount;

        if (input.OutputFileHandling == "original")
        {
            var targetContent = content.Target();
            result.File = await fileManagementClient.UploadAsync(targetContent.Serialize().ToStream(), targetContent.OriginalMediaType, targetContent.OriginalName);
        } else
        {
            result.File = await fileManagementClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff, content.XliffFileName);
        }        

        return result;
    }

    [BlueprintActionDefinition(BlueprintAction.EditText)]
    [Action("Edit text", Description = "Review translated text and generate an edited version")]
    public async Task<EditResponse> PostEditRequest([ActionParameter] TextChatModelIdentifier modelIdentifier,
    [ActionParameter] PostEditRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var systemPrompt =
            $"You are receiving a source text{(input.SourceLanguage != null ? $" written in {input.SourceLanguage} " : "")}" +
            $"that was translated into target text{(input.TargetLanguage != null ? $" written in {input.TargetLanguage}" : "")}. " +
            "Review the target text and respond with edits of the target text as necessary. If no edits required, respond with target text. " +
            $"{(input.TargetAudience != null ? $"The target audience is {input.TargetAudience}" : string.Empty)}";


        if (glossary.Glossary != null)
            systemPrompt +=
                " Enhance the target text by incorporating relevant terms from our glossary where applicable. " +
                "Ensure that the translation aligns with the glossary entries for the respective languages. " +
                "If a term has variations or synonyms, consider them and choose the most appropriate " +
                "translation to maintain consistency and precision. If the translation already aligns " +
                "with the glossary, no edits are required.";

        if (input.AdditionalPrompt != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalPrompt}";

        var userPrompt = @$"
            Source text: 
            {input.SourceText}

            Target text: 
            {input.TargetText}
        ";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.SourceText, true);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteChatCompletion(messages, modelIdentifier.GetModel());
        return new EditResponse
        {
            UserPrompt = userPrompt,
            SystemPrompt = systemPrompt,
            EditedText = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }
}
