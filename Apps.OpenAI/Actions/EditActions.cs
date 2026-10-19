using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Models.Content;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Ai.Content.Models;
using Blackbird.Ai.Diagnostics.Models;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Summary.Models;
using Blackbird.Ai.Text.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System;
using System.IO;
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
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Responses.Background;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Apps.OpenAI.Extensions;
using SegmentStateSets = Blackbird.Ai.Segments.SegmentStateSets;

namespace Apps.OpenAI.Actions;

[ActionList("Editing")]
public class EditActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseActions(invocationContext, fileManagementClient)
{
    [BlueprintActionDefinition(BlueprintAction.EditFile)]
    [Action("Edit", Description = "Edits translated file content and outputs reviewed content.")]
    public async Task<ContentProcessingEditResult> EditContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] EditContentRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] ReasoningEffortRequest reasoningEffortRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the maximum number of source texts to be edited at once. Default value: 250. Buckets are also sized to fit the model's output limit. (See our documentation for an explanation)")] int? bucketSize = null,
        [ActionParameter, Display("Process locked segments")] bool? ProcessLockedSegments = null)
    {
        var effectiveModel = await ResolveTextChatModelAsync(modelIdentifier.ModelId);
        var client = CreateLlmClient(effectiveModel, new OpenAiLlmSettings(reasoningEffortRequest.ReasoningEffort, input.MaxTokens));
        var processor = CreateContentProcessor(client, effectiveModel, bucketSize);

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(glossary.Glossary);

        var result = await AiAdapters.Mapped(() =>
            processor.EditAsync(EditOptions(input, file, glossaryFile, prompt, ProcessLockedSegments)));

        var content = result.Content;

        return new ContentProcessingEditResult
        {
            File = await UploadAsync(result.File),
            Usage = ToUsageDto(content.Usage, client, effectiveModel),
            TotalSegmentsCount = content.TotalSegments,
            TotalWordsCount = content.TotalWords,
            TotalSegmentsReviewed = content.ProcessableSegments,
            TotalWordsReviewed = content.ProcessableWords,
            TotalSegmentsUpdated = content.UpdatedSegments,
            TotalWordsUpdated = content.UpdatedWords,
            TotalSegmentsWithErrors = content.Issues
                .Where(issue => issue.Kind == IssueKind.SegmentWriteFailed)
                .Sum(issue => issue.SegmentIds.Count),
            ProcessedBatchesCount = content.BucketsProcessed,
            SystemPrompt = content.SystemPrompt ?? string.Empty,
            ErrorDetails = content.Warnings.ToList()
        };
    }

    [Action("Edit in background",
        Description = "Starts background editing of translated content and outputs a batch ID and a transformation file to download the result later.")]
    public async Task<BackgroundProcessingResponse> EditInBackground([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] EditContentRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify additional instructions to be applied to the translation. For example, 'Cater to an older audience.'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] ReasoningEffortRequest reasoningEffortRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the maximum number of source texts to be edited in one request of the batch. Default value: 250. Buckets are also sized to fit the model's output limit. (See our documentation for an explanation)")] int? bucketSize = null,
        [ActionParameter, Display("Process locked segments")] bool? ProcessLockedSegments = null)
    {
        var effectiveModel = await ResolveTextChatModelAsync(modelIdentifier.ModelId);
        var settings = new OpenAiLlmSettings(reasoningEffortRequest.ReasoningEffort, input.MaxTokens);
        var client = CreateLlmClient(effectiveModel, settings);
        var processor = CreateBackgroundProcessor(CreateBatchClient(effectiveModel, settings), client, effectiveModel, bucketSize);

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(glossary.Glossary);

        var started = await AiAdapters.Mapped(() =>
            processor.StartEditAsync(EditOptions(input, file, glossaryFile, prompt, ProcessLockedSegments)));

        return await ToBackgroundResponseAsync(started, client, effectiveModel);
    }

    private static EditStreamOptions EditOptions(
        EditContentRequest input,
        Stream file,
        Stream? glossaryFile,
        string? prompt,
        bool? processLockedSegments) => new()
    {
        File = file,
        FileName = input.File.Name,
        MediaType = input.File.ContentType,
        SourceLanguage = input.SourceLanguage,
        TargetLanguage = input.TargetLanguage,
        OutputFileHandling = input.OutputFileHandling,
        GlossaryTbx = glossaryFile,
        FilterGlossary = input.FilterGlossary ?? true,
        AdditionalInstructions = prompt,
        ModifiedBy = input.ModifiedBy,
        SegmentStates = SegmentStateSets.Parse(input.ProcessOnlySegmentState, SegmentStateSets.Edit),
        ProcessLockedSegments = processLockedSegments ?? false,
        TransformTarget = EditTransform,
        Summary = SummaryOptions.For(input.DocumentSummary)
    };

    // Background downloads apply edits with the same post-processing as the synchronous Edit action.
    internal static string? EditTransform(TargetCandidate candidate) => EscapeInlineTagBrackets(candidate.ProposedTarget);

    [BlueprintActionDefinition(BlueprintAction.EditText)]
    [Action("Edit text", Description = "Reviews translated text and outputs an edited version.")]
    public async Task<EditResponse> PostEditRequest([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] PostEditRequest input, 
        [ActionParameter] GlossaryRequest glossary)
    {
        var client = CreateLlmClient(modelIdentifier.ModelId, new OpenAiLlmSettings(input.ReasoningEffort));
        var processor = CreateTextProcessor(client, modelIdentifier.ModelId);

        await using var glossaryFile = await DownloadOptionalAsync(glossary.Glossary);

        var result = await AiAdapters.Mapped(() => processor.EditAsync(new EditTextOptions
        {
            SourceText = input.SourceText,
            TargetText = input.TargetText,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            AdditionalInstructions = EditInstructions(input),
            GlossaryTbx = glossaryFile
        }));

        if (result.Content.UnresolvedSegments > 0)
            throw new PluginApplicationException(string.Join(Environment.NewLine, result.Content.Warnings));

        return new EditResponse
        {
            UserPrompt = LastUserPrompt(result.Content),
            SystemPrompt = result.Content.SystemPrompt ?? string.Empty,
            EditedText = result.Text,
            Usage = ToUsageDto(result.Content.Usage, client, modelIdentifier.ModelId)
        };
    }

    private static string? EditInstructions(PostEditRequest input)
    {
        var instructions = new[]
            {
                string.IsNullOrWhiteSpace(input.TargetAudience) ? null : $"The target audience is {input.TargetAudience}.",
                input.AdditionalPrompt
            }
            .Where(instruction => !string.IsNullOrWhiteSpace(instruction))
            .ToList();

        return instructions.Count == 0 ? null : string.Join(" ", instructions);
    }

    [Action("Apply prompt to bilingual content (experimental)",
        Description = "Applies a prompt to each translation unit and outputs updated target text, with optional batching.")]
    public async Task<ContentProcessingEditResult> Prompt(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] EditContentRequest input,
        [ActionParameter, Display("System prompt")] string systemPrompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] ReasoningEffortRequest reasoningEffortRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be edited at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = 1500)
    {
        var effectiveModel = await ResolveTextChatModelAsync(modelIdentifier.ModelId);
        var result = new ContentProcessingEditResult();

        var neverFail = false;
        var batchSize = bucketSize ?? 1500;

        var inputFileStream = await fileManagementClient.DownloadAsync(input.File);
        var loadResult = Transformation.Load(inputFileStream, input.File.Name, input.File.ContentType);
        if (!loadResult.Success)
            throw new PluginMisconfigurationException(loadResult.Error);

        var content = loadResult.Value;

        var batchProcessingService = new BatchProcessingService(UniversalClient, FileManagementClient);
        var batchOptions = new BatchProcessingOptions(
            effectiveModel,
            content.SourceLanguage,
            content.TargetLanguage,
            Prompt: string.Empty,
            systemPrompt,
            OverwritePrompts: true,
            glossary.Glossary,
            FilterGlossary: true,
            MaxRetryAttempts: 3,
            MaxTokens: null,
            reasoningEffortRequest.ReasoningEffort,
            content.Notes,
            content.GetTerms().ToList());

        var errors = new List<string>();
        var usages = new List<UsageDto>();
        int batchCounter = 0;

        var systemprompt = string.Empty;

        async Task<IEnumerable<TranslationEntity>> BatchTranslate(IEnumerable<(Unit Unit, Segment Segment)> batch)
        {
            var batchList = batch.ToList();
            var idSegments = batchList.Select((x, i) => new { Id = i + 1, Value = x }).ToDictionary(x => x.Id.ToString(), x => x.Value.Segment);
            batchCounter++;

            var translationLookup = new Dictionary<string, TranslationEntity>();
            try
            {
                var batchResult = await batchProcessingService.ProcessBatchAsync(idSegments, batchOptions, postEdit: true);

                systemprompt = batchResult.SystemPrompt;

                var duplicates = batchResult.UpdatedTranslations.GroupBy(x => x.TranslationId)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { TranslationId = g.Key, Count = g.Count() })
                    .ToList();

                errors.AddRange(duplicates.Select(duplicate => $"Duplicate translation ID found: {duplicate.TranslationId} appears {duplicate.Count} times"));
                errors.AddRange(batchResult.ErrorMessages);
                usages.Add(batchResult.Usage);

                if (batchResult.IsSuccess)
                {
                    foreach (var translation in batchResult.UpdatedTranslations)
                    {
                        translationLookup.TryAdd(translation.TranslationId, translation);
                    }
                }

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

            // Ensure exactly one result per (Unit, Segment) in the batch
            var allResults = new List<TranslationEntity>();
            for (int i = 0; i < batchList.Count; i++)
            {
                var id = (i + 1).ToString();
                if (translationLookup.TryGetValue(id, out var translation))
                {
                    allResults.Add(translation);
                }
                else
                {
                    allResults.Add(new TranslationEntity
                    {
                        TranslationId = id,
                        TranslatedText = batchList[i].Segment.GetTarget()
                    });
                }
            }

            return allResults;
        }

        var allUnits = content.GetUnits().ToList();
        var allSegments = allUnits.SelectMany(x => x.Segments).ToList();
        result.TotalSegmentsCount = allSegments.Count;
        result.TotalWordsCount = allSegments.Sum(segment => segment.Source.CountWords());
        
        var statesToProcess = GetProcessingStates(input.ProcessOnlySegmentState, SegmentState.Initial, SegmentState.Translated);
        var segmentsToReview = allSegments
            .Where(segment => segment.ShouldBeProcessed(statesToProcess))
            .ToList();

        result.TotalSegmentsReviewed = segmentsToReview.Count;
        result.TotalWordsReviewed = segmentsToReview.Sum(segment => segment.Source.CountWords());

        var processedBatches = await allUnits
            .Batch(batchSize, segment => segment.ShouldBeProcessed(statesToProcess))
            .Process(BatchTranslate);
        result.ProcessedBatchesCount = batchCounter;
        result.Usage = UsageDto.Sum(usages);
        result.SystemPrompt = systemprompt;

        int updatedSegmentsCount = 0;
        int updatedWordsCount = 0;

        foreach (var (unit, results) in processedBatches)
        {
            foreach (var (segment, translation) in results)
            {
                if (segment.GetTarget() != translation.TranslatedText)
                {
                    updatedSegmentsCount++;
                    segment.SetTarget(translation.TranslatedText);
                    updatedWordsCount += segment.Source.CountWords();
                }
                segment.State = SegmentState.Reviewed;
            }

            var model = effectiveModel;
            unit.Provenance.Review.Tool = model;
            double tokens = result.Usage.TotalTokens / processedBatches.Count();
            unit.AddUsage(model, Math.Round(tokens, 0), UsageUnit.Tokens);
        }

        result.TotalSegmentsUpdated = updatedSegmentsCount;
        result.TotalWordsUpdated = updatedWordsCount;
        
        result.File = await OutputFileHandler.ToOutputFile(FileManagementClient, content, input.OutputFileHandling);

        return result;
    }

    private static HashSet<SegmentState> GetProcessingStates(IEnumerable<string>? states, params SegmentState[] defaultStates)
    {
        var parsedStates = states?
            .Select(state => Enum.TryParse<SegmentState>(state, true, out var parsedState)
                ? parsedState
                : (SegmentState?)null)
            .Where(state => state.HasValue)
            .Select(state => state!.Value)
            .ToHashSet();

        if (parsedStates is { Count: > 0 })
        {
            return parsedStates;
        }

        return defaultStates.ToHashSet();
    }

    private static string EscapeInlineTagBrackets(string text)
    {
        text = Regex.Replace(text, @"\{(\d+)>", "{$1gt;");
        text = Regex.Replace(text, @"<(\d+)\}", "lt;$1}");
        return text;
    }
}
