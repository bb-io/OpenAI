using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Models.Content;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Ai.Content.Models;
using Blackbird.Ai.Diagnostics.Models;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Llm.Abstract;
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

        return await ToEditResultAsync(result, client, effectiveModel);
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

    [Action("Apply prompt to bilingual content (experimental)",
        Description = "Applies a prompt to each translation unit and outputs updated target text, with optional batching.")]
    public async Task<ContentProcessingEditResult> Prompt(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] EditContentRequest input,
        [ActionParameter, Display("System prompt", Description = "The instruction applied to every selected segment. The model sees each segment's source and current target and returns the new target.")] string systemPrompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] ReasoningEffortRequest reasoningEffortRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the maximum number of segments sent in one request. Default value: 250. Buckets are also sized to fit the model's output limit. (See our documentation for an explanation)")] int? bucketSize = null)
    {
        var effectiveModel = await ResolveTextChatModelAsync(modelIdentifier.ModelId);
        var client = CreateLlmClient(effectiveModel, new OpenAiLlmSettings(reasoningEffortRequest.ReasoningEffort, input.MaxTokens));
        var processor = CreateContentProcessor(client, effectiveModel, bucketSize);

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(glossary.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ApplyPromptAsync(new ApplyPromptStreamOptions
        {
            File = file,
            FileName = input.File.Name,
            MediaType = input.File.ContentType,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            OutputFileHandling = input.OutputFileHandling,
            GlossaryTbx = glossaryFile,
            FilterGlossary = input.FilterGlossary ?? true,
            Prompt = systemPrompt,
            SegmentStates = SegmentStateSets.Parse(input.ProcessOnlySegmentState, SegmentStateSets.ApplyPrompt),
            StateAfterWrite = SegmentState.Reviewed,
            Summary = input.DocumentSummary is null ? SummaryOptions.Never : SummaryOptions.For(input.DocumentSummary)
        }));

        return await ToEditResultAsync(result, client, effectiveModel);
    }

    // Background downloads apply edits with the same post-processing as the synchronous Edit action.
    internal static string? EditTransform(TargetCandidate candidate) => EscapeInlineTagBrackets(candidate.ProposedTarget);

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

    private async Task<ContentProcessingEditResult> ToEditResultAsync(ContentStreamResult result, ILlmClient client, string? model)
    {
        var content = result.Content;

        return new ContentProcessingEditResult
        {
            File = await UploadAsync(result.File),
            Usage = ToUsageDto(content.Usage, client, model),
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

    private static string EscapeInlineTagBrackets(string text)
    {
        text = Regex.Replace(text, @"\{(\d+)>", "{$1gt;");
        text = Regex.Replace(text, @"<(\d+)\}", "lt;$1}");
        return text;
    }
}
