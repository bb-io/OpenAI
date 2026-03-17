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
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Utils;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Responses.Background;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using Blackbird.Filters.Xliff.Xliff1;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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
        [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be edited at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = null,
        [ActionParameter, Display("Process locked segments")] bool? ProcessLockedSegments = null)
    {
        var neverFail = false;
        var batchSize = bucketSize ?? 1500;
        var result = new ContentProcessingEditResult();
        var stream = await fileManagementClient.DownloadAsync(input.File);

        var content = await ErrorHandler.ExecuteWithErrorHandlingAsync(() =>
            Transformation.Parse(stream, input.File.Name)
        );

        var sourceLanguage = input.SourceLanguage ?? content.SourceLanguage;
        var targetLanguage = input.TargetLanguage ?? content.TargetLanguage;

        var filterGlossary = input.FilterGlossary ?? true;

        var batchProcessingService = new BatchProcessingService(UniversalClient, FileManagementClient);
        var batchOptions = new BatchProcessingOptions(
            UniversalClient.GetModel(modelIdentifier.ModelId),
            sourceLanguage,
            targetLanguage,
            prompt,
            string.Empty,
            false,
            glossary.Glossary,
            filterGlossary,
            3,
            input.MaxTokens,
            reasoningEffortRequest.ReasoningEffort,
            content.Notes);

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
                var batchResult = await batchProcessingService.ProcessBatchAsync(idSegments, batchOptions, true);

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
                    // Fallback: return original target text so the segment is unchanged
                    allResults.Add(new TranslationEntity
                    {
                        TranslationId = id,
                        TranslatedText = batchList[i].Segment.GetTarget()
                    });
                }
            }

            return allResults;
        }

        var units = content.GetUnits();
        var segments = units.SelectMany(x => x.Segments);
        result.TotalSegmentsCount = segments.Count();

        if (!string.IsNullOrEmpty(input.ProcessOnlySegmentState) && Enum.TryParse<SegmentState>(input.ProcessOnlySegmentState, out var filterState))
        {
            units = units.Where(x => x.State == filterState);
        }
        else
        {
            units = units.Where(x => x.State == SegmentState.Translated);
        }

        if (ProcessLockedSegments == null || ProcessLockedSegments == false) 
        {
            units = units.Where(x => x.Translate == null || x.Translate == true);
        }
        
        segments = units.SelectMany(x => x.Segments);
        result.TotalSegmentsReviewed = segments.Count();

        var processedBatches = await units.Batch(batchSize).Process(BatchTranslate);
        result.ProcessedBatchesCount = batchCounter;
        result.Usage = UsageDto.Sum(usages);
        result.SystemPrompt = systemprompt;

        var updatedCount = 0;

        foreach (var (unit, results) in processedBatches)
        {
            var modifiedSegment = false;
            foreach (var (segment, translation) in results)
            {
                var sanitizedText = EscapeInlineTagBrackets(translation.TranslatedText);
                if (segment.GetTarget() != sanitizedText)
                {
                    try
                    {
                        segment.SetTarget(sanitizedText);
                        segment.State = SegmentState.Reviewed;
                        updatedCount++;
                        modifiedSegment = true;
                    }
                    catch(Exception ex)
                    {
                        errors.Add($"Error updating segment with ID {segment.Id}: {ex.Message}");
                        result.TotalSegmentsWithErrors += 1;
                    }
                }
                else 
                {
                    segment.State = SegmentState.Reviewed;
                }
            }

            var model = UniversalClient.GetModel(modelIdentifier.ModelId);
            unit.Provenance.Review.Tool = model;
            double tokens = result.Usage.TotalTokens / processedBatches.Count();
            unit.AddUsage(model, Math.Round(tokens, 0), UsageUnit.Tokens);
            
            if (!string.IsNullOrEmpty(input.ModifiedBy) && modifiedSegment)
            {
                long unixTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var existingModifiedAttr = unit.Other.OfType<XAttribute>()
                    .FirstOrDefault(x => x.Name.LocalName == "modified-at" || x.Name.LocalName == "modified-by");
                
                var ns = existingModifiedAttr?.Name.Namespace ?? XNamespace.None;
                unit.Other.RemoveAll(x => x is XAttribute attr && 
                    (attr.Name.LocalName == "modified-at" || attr.Name.LocalName == "modified-by"));
                
                unit.Other.Add(new XAttribute(ns + "modified-at", unixTimestampMs.ToString()));
                unit.Other.Add(new XAttribute(ns + "modified-by", input.ModifiedBy));
            }
        }

        result.TotalSegmentsUpdated = updatedCount;

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

        result.ErrorDetails = errors;
        return result;
    }

    [Action("Edit in background", 
        Description = "Starts background editing for translated content and outputs a batch ID to download results later.")]
    public async Task<BackgroundProcessingResponse> EditInBackground(
        [ActionParameter] StartBackgroundProcessRequest processRequest)
    {
        var stream = await fileManagementClient.DownloadAsync(processRequest.File);
        var content = await ErrorHandler.ExecuteWithErrorHandlingAsync(() => 
            Transformation.Parse(stream, processRequest.File.Name)
        );

        var units = content.GetUnits();
        var segments = units.SelectMany(x => x.Segments).GetSegmentsForEditing().ToList();

        Glossary blackbirdGlossary = await ProcessGlossaryFromFile(processRequest.Glossary);
        Dictionary<string, List<GlossaryEntry>> glossaryLookup = null;
        if (blackbirdGlossary != null)
        {
            glossaryLookup = CreateGlossaryLookup(blackbirdGlossary);
        }

        string systemPromptBase = 
            "You are receiving source texts that were translated into target texts. " +
            "Review the target texts and respond with edits of the target texts as necessary. " +
            "If no edits required, respond with the original target texts. " +
            "Return the edits in the specified JSON format." +
            "The JSON must strictly follow this structure: " +
            "{ \"reports\": [ { \"segment_id\": \"(string matching custom_id)\", \"mqm_report\": \"(the edited text)\" } ] }";
                          
        if (processRequest.AdditionalInstructions != null)
        {
            systemPromptBase += $" Additional instructions: {processRequest.AdditionalInstructions}.";
        }
        
        if (glossaryLookup != null)
        {
            systemPromptBase += " Use the provided glossary to ensure accurate translations of specific terms.";
        }

        var batchRequests = new List<object>();
        var bucketSize = processRequest.GetBucketingSize();
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
            var userPrompt = "Review and edit the following texts:\n\n";
            
            foreach (var (segment, segmentIndex) in bucket.Select((seg, idx) => (seg, idx)))
            {
                var globalIndex = bucketIndex * bucketSize + segmentIndex;
                var sourceText = segment.GetSource();
                var targetText = segment.GetTarget();
                
                userPrompt += $"ID: {globalIndex}\nSource text: {sourceText}\nTarget text: {targetText}\n\n";
            }
            
            if (glossaryLookup != null)
            {
                var combinedText = string.Join(" ", bucket.Select(s => s.GetSource()));
                var glossaryPromptPart = GetOptimizedGlossaryPromptPart(glossaryLookup, combinedText);
                if (!string.IsNullOrEmpty(glossaryPromptPart))
                {
                    userPrompt += $"\nGlossary terms:\n{glossaryPromptPart}";
                }
            }

            string modelId = UniversalClient.GetModel(processRequest.ModelId);
            var chatInput = new BaseChatRequest
            {
                MaximumTokens = processRequest.MaximumTokens,
                Temperature = 0.3f
            };

            var messages = new object[]
            {
                new { role = MessageRoles.System, content = systemPromptBase },
                new { role = MessageRoles.User, content = userPrompt }
            };

            var bodyDict = GenerateResponseBody(messages, modelId, chatInput);
            var batchRequest = new
            {
                custom_id = bucketIndex.ToString(),
                method = "POST",
                url = "/v1/responses",
                body = bodyDict
            };

            batchRequests.Add(batchRequest);
        }

        var batchResponse = await CreateBatchAsync(batchRequests);
        content.MetaData.Add(new Metadata("background-type", "edit") { Category = [Meta.Categories.Blackbird]});
        return new BackgroundProcessingResponse
        {
            BatchId = batchResponse.Id,
            Status = batchResponse.Status,
            CreatedAt = batchResponse.CreatedAt,
            ExpectedCompletionTime = batchResponse.ExpectedCompletionTime,
            TransformationFile = await fileManagementClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff, content.XliffFileName)
        };
    }
    
    [BlueprintActionDefinition(BlueprintAction.EditText)]
    [Action("Edit text", Description = "Reviews translated text and outputs an edited version.")]
    public async Task<EditResponse> PostEditRequest([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] PostEditRequest input, 
        [ActionParameter] GlossaryRequest glossary)
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
        var response = await ExecuteApiRequest(messages, UniversalClient.GetModel(modelIdentifier.ModelId), new() { ReasoningEffort = input.ReasoningEffort});
        return new EditResponse
        {
            UserPrompt = userPrompt,
            SystemPrompt = systemPrompt,
            EditedText = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
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
        var result = new ContentProcessingEditResult();

        var neverFail = false;
        var batchSize = bucketSize ?? 1500;

        var inputFileStream = await fileManagementClient.DownloadAsync(input.File);
        var content = await ErrorHandler.ExecuteWithErrorHandlingAsync(() => 
            Transformation.Parse(inputFileStream, input.File.Name)
        );

        var batchProcessingService = new BatchProcessingService(UniversalClient, FileManagementClient);
        var batchOptions = new BatchProcessingOptions(
            UniversalClient.GetModel(modelIdentifier.ModelId),
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
            content.Notes);

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

        var allUnits = content.GetUnits();
        var allSegments = allUnits.SelectMany(x => x.Segments);
        result.TotalSegmentsCount = allSegments.Count();

        var translatedUnits = allUnits.Where(x => x.State == SegmentState.Translated);
        var translatedSegments = translatedUnits.SelectMany(x => x.Segments);
        result.TotalSegmentsReviewed = translatedSegments.Count();

        var processedBatches = await translatedUnits.Batch(batchSize).Process(BatchTranslate);
        result.ProcessedBatchesCount = batchCounter;
        result.Usage = UsageDto.Sum(usages);
        result.SystemPrompt = systemprompt;

        var updatedCount = 0;

        foreach (var (unit, results) in processedBatches)
        {
            foreach (var (segment, translation) in results)
            {
                if (segment.GetTarget() != translation.TranslatedText)
                {
                    updatedCount++;
                    segment.SetTarget(translation.TranslatedText);
                }
                segment.State = SegmentState.Reviewed;
            }

            var model = UniversalClient.GetModel(modelIdentifier.ModelId);
            unit.Provenance.Review.Tool = model;
            double tokens = result.Usage.TotalTokens / processedBatches.Count();
            unit.AddUsage(model, Math.Round(tokens, 0), UsageUnit.Tokens);
        }

        result.TotalSegmentsUpdated = updatedCount;

        if (input.OutputFileHandling == "original")
        {
            var targetContent = content.Target();
            result.File = await fileManagementClient.UploadAsync(
                targetContent.Serialize().ToStream(),
                targetContent.OriginalMediaType,
                targetContent.OriginalName);
        }
        else if (input.OutputFileHandling == "xliff1")
        {
            result.File = await fileManagementClient.UploadAsync(
                Xliff1Serializer.Serialize(content).ToStream(),
                MediaTypes.Xliff,
                content.XliffFileName);
        }
        else
        {
            result.File = await fileManagementClient.UploadAsync(
                content.Serialize().ToStream(),
                MediaTypes.Xliff,
                content.XliffFileName);
        }

        return result;
    }

    private static string EscapeInlineTagBrackets(string text)
    {
        text = Regex.Replace(text, @"\{(\d+)>", "{$1gt;");
        text = Regex.Replace(text, @"<(\d+)\}", "lt;$1}");
        return text;
    }
}
