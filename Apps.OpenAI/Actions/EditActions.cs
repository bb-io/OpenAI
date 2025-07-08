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

namespace Apps.OpenAI.Actions;

[ActionList("Editing")]
public class EditActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseActions(invocationContext, fileManagementClient)
{

    [BlueprintActionDefinition(BlueprintAction.EditFile)]
    [Action("Edit", Description = "[Experimental] Edit a content file. Only supports XLIFF input (received from any action that returns bilingual).")]
    public async Task<ContentProcessingEditResult> EditContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] EditContentRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be translated at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = null)
    {
        var neverFail = false;
        var batchSize = bucketSize ?? 1500;
        var result = new ContentProcessingEditResult();
        var stream = await fileManagementClient.DownloadAsync(input.File);
        var content = await Transformation.Parse(stream, input.File.Name);

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
        }
        result.File = await fileManagementClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff, content.XliffFileName);

        return result;
    }
}
