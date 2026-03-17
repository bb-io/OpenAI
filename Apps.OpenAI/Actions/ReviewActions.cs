using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Review;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Models.Responses.Review;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Apps.OpenAI.Actions;

[ActionList("Review")]
public class ReviewActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Get translation issues", Description = "Reviews translated text and outputs issue descriptions.")]
    public async Task<ChatResponse> GetTranslationIssues([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] GetTranslationIssuesRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var systemPrompt =
            $"You are receiving a source text{(input.SourceLanguage != null ? $" written in {input.SourceLanguage} " : "")}" +
            $"that was translated by NMT into target text{(input.TargetLanguage != null ? $" written in {input.TargetLanguage}" : "")}. " +
            "Evaluate the target text for grammatical errors, language structure issues, and overall linguistic coherence, " +
            "including them in the issues description. Respond with the issues description. " +
            $"{(input.TargetAudience != null ? $"The target audience is {input.TargetAudience}" : string.Empty)}";


        if (glossary.Glossary != null)
            systemPrompt +=
                " Ensure that the translation aligns with the glossary entries provided for the respective " +
                "languages, and note any discrepancies, ambiguities, or incorrect usage of terms. Include " +
                "these observations in the issues description.";

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
        var response = await ExecuteApiRequest(messages, modelIdentifier.ModelId);

        return new()
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Get MQM dimension values",
        Description =
            "Performs MQM analysis for translated text and outputs per-dimension scores with a proposed translation.")]
    public async Task<MqmAnalysis> GetLqaDimensionValues([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] GetTranslationIssuesRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var systemPrompt = "Perform an LQA analysis and use the MQM error typology format using all 7 dimensions. " +
                           "Here is a brief description of the seven high-level error type dimensions: " +
                           "1. Terminology – errors arising when a term does not conform to normative domain or organizational terminology standards or when a term in the target text is not the correct, normative equivalent of the corresponding term in the source text. " +
                           "2. Accuracy – errors occurring when the target text does not accurately correspond to the propositional content of the source text, introduced by distorting, omitting, or adding to the message. " +
                           "3. Linguistic conventions  – errors related to the linguistic well-formedness of the text, including problems with grammaticality, spelling, punctuation, and mechanical correctness. " +
                           "4. Style – errors occurring in a text that are grammatically acceptable but are inappropriate because they deviate from organizational style guides or exhibit inappropriate language style. " +
                           "5. Locale conventions – errors occurring when the translation product violates locale-specific content or formatting requirements for data elements. " +
                           "6. Audience appropriateness – errors arising from the use of content in the translation product that is invalid or inappropriate for the target locale or target audience. " +
                           "7. Design and markup – errors related to the physical design or presentation of a translation product, including character, paragraph, and UI element formatting and markup, integration of text with graphical elements, and overall page or window layout. " +
                           "Provide a quality rating for each dimension from 0 (completely bad) to 10 (perfect). You are an expert linguist and your task is to perform a Language Quality Assessment on input sentences. " +
                           "Try to propose a fixed translation that would have no LQA errors. " +
                           "The response should be in the following json format: " +
                           "{\r\n  \"terminology\": 0,\r\n  \"accuracy\": 0,\r\n  \"linguistic_conventions\": 0,\r\n  \"style\": 0,\r\n  \"locale_conventions\": 0,\r\n  \"audience_appropriateness\": 0,\r\n  \"design_and_markup\": 0,\r\n  \"proposed_translation\": \"fixed translation\"\r\n}";

        if (glossary.Glossary != null)
            systemPrompt += " Use the provided glossary entries for the respective languages. If there are " +
                            "discrepancies between the translation and glossary, reduce the score in the " +
                            "'Terminology' part of the report respectively.";

        if (input.AdditionalPrompt != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalPrompt}";

        var userPrompt =
            $"{(input.SourceLanguage != null ? $"The {input.SourceLanguage} " : "")}\"{input.SourceText}\" was translated as \"{input.TargetText}\"{(input.TargetLanguage != null ? $" into {input.TargetLanguage}" : "")}.{(input.TargetAudience != null ? $" The target audience is {input.TargetAudience}" : "")}";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.SourceText, true);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteApiRequest(messages, modelIdentifier.ModelId, input, new { type = "json_object" });

        try
        {
            var analysis = JsonConvert.DeserializeObject<MqmAnalysis>(response.Choices.First().Message.Content);
            analysis.Usage = response.Usage;
            return analysis;
        }
        catch
        {
            throw new Exception(
                "Something went wrong parsing the output from OpenAI, most likely due to a hallucination!");
        }
    }

    [BlueprintActionDefinition(BlueprintAction.ReviewText)]
    [Action("Review text", Description = "Reviews translated text quality and outputs a quality score.")]
    public async Task<ReviewTextResponse> ReviewText([ActionParameter] ReviewTextRequest input)
    {
        var reviewData = new[]
        {
            new
            {
                translation_id = "1",
                source_text = input.SourceText,
                target_text = input.TargetText
            }
        };

        var json = JsonConvert.SerializeObject(reviewData);

        var systemPrompt = PromptBuilder.BuildReviewSystemPrompt();
        var userPrompt = PromptBuilder.BuildReviewUserPrompt(
            input.AdditionalInstructions,
            input.SourceLanguage,
            input.TargetLanguage,
            json);

        if (input.Glossary != null)
        {
            var glossaryPart = await GetGlossaryPromptPart(input.Glossary, input.SourceText, true);
            if (!string.IsNullOrWhiteSpace(glossaryPart))
                userPrompt += glossaryPart;
        }

        var messages = new List<ChatMessageDto>
        {
            new(MessageRoles.System, systemPrompt),
            new(MessageRoles.User, userPrompt)
        };

        var response = await ExecuteApiRequest(
            messages,
            model: input.Model,
            input: null,
            responseFormat: new { type = "json_object" });

        var raw = response.Choices.First().Message.Content;

        float score = 0f;
        try
        {
            var parsed = JsonConvert.DeserializeObject<ReviewJsonResponse>(raw);
            score = parsed?.Translations?.FirstOrDefault()?.QualityScore ?? 0f;
        }
        catch
        {
            score = 0f;
        }

        score = score < 0 ? 0 : (score > 1 ? 1 : score);

        return new ReviewTextResponse
        {
            Score = score,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Usage = MapUsage(response.Usage)
        };
    }

    [BlueprintActionDefinition(BlueprintAction.ReviewFile)]
    [Action("Review", Description = "Reviews translated file content and outputs segment quality scores.")]
    public async Task<ReviewContentResponse> ReviewContent([ActionParameter] ReviewContentRequest input)
    {
        var result = new ReviewContentResponse();

        var threshold = input.Threshold ?? 0.8;
        if (threshold < 0 || threshold > 1)
            throw new PluginMisconfigurationException("Threshold must be in range 0..1.");

        var stream = await fileManagementClient.DownloadAsync(input.File);
        var content =
            await ErrorHandler.ExecuteWithErrorHandlingAsync(() => Transformation.Parse(stream, input.File.Name));

        content.SourceLanguage ??= input.SourceLanguage;
        content.TargetLanguage ??= input.TargetLanguage;

        if (string.IsNullOrWhiteSpace(content.SourceLanguage))
            throw new PluginMisconfigurationException(
                "The source language is not defined. Please assign the source language in this action.");

        if (string.IsNullOrWhiteSpace(content.TargetLanguage))
            throw new PluginMisconfigurationException(
                "The target language is not defined. Please assign the target language in this action.");

        var processedSegmentsCount = 0;
        var finalizedSegmentsCount = 0;
        var riskySegmentsCount = 0;
        float totalScore = 0f;

        async Task<IEnumerable<float?>> BatchProcess(IEnumerable<(Unit Unit, Segment Segment)> batch)
        {
            var scores = new List<float?>();

            foreach (var (_, segment) in batch)
            {
                if (string.IsNullOrWhiteSpace(segment.GetTarget()))
                {
                    scores.Add(null);
                    continue;
                }

                var reviewData = new[]
                {
                new
                {
                    translation_id = segment.Id,
                    source_text = segment.GetSource(),
                    target_text = segment.GetTarget()
                }
            };

                var json = JsonConvert.SerializeObject(reviewData);

                var systemPrompt = PromptBuilder.BuildReviewSystemPrompt();
                var userPrompt = PromptBuilder.BuildReviewUserPrompt(
                    input.AdditionalInstructions,
                    content.SourceLanguage,
                    content.TargetLanguage!,
                    json);

                if (input.Glossary != null)
                {
                    var glossaryPart = await GetGlossaryPromptPart(input.Glossary, segment.GetSource(), true);
                    if (!string.IsNullOrWhiteSpace(glossaryPart))
                        userPrompt += glossaryPart;
                }

                var messages = new List<ChatMessageDto>
            {
                new(MessageRoles.System, systemPrompt),
                new(MessageRoles.User, userPrompt)
            };

                var response = await ExecuteApiRequest(
                    messages,
                    model: input.Model,
                    input: null,
                    responseFormat: new { type = "json_object" });

                result.Usage += MapUsage(response.Usage);

                var raw = response.Choices.First().Message.Content;

                float score;
                try
                {
                    var parsed = JsonConvert.DeserializeObject<ReviewJsonResponse>(raw);
                    score = parsed?.Translations?.FirstOrDefault()?.QualityScore ?? 0f;
                }
                catch
                {
                    score = 0f;
                }

                if (score < 0f) score = 0f;
                if (score > 1f) score = 1f;

                scores.Add(score);
            }

            return scores;
        }

        var units = await content.GetUnits()
            .Batch(10, x => !x.IsIgnorbale && !x.IsInitial && x.State != SegmentState.Final)
            .Process(BatchProcess);

        foreach (var (unit, results) in units)
        {
            float unitScore = 0f;
            var unitCount = 0;

            foreach (var (segment, score) in results)
            {
                if (score == null) continue;

                processedSegmentsCount++;
                totalScore += score.Value;
                unitScore += score.Value;
                unitCount++;

                segment.TargetAttributes.RemoveAll(attr => attr.Name == "extradata");
                segment.TargetAttributes.Add(new XAttribute(
                    "extradata",
                    score.Value.ToString(CultureInfo.InvariantCulture)));

                if (score.Value >= threshold)
                {
                    segment.State = SegmentState.Final;
                    finalizedSegmentsCount++;
                }
                else
                {
                    riskySegmentsCount++;
                }
            }

            unit.Quality.ProfileReference = "OpenAI review";
            unit.Quality.ScoreThreshold = threshold;
            unit.Quality.Score = unitCount > 0 ? (unitScore / unitCount) : 0f;
        }

        Stream streamResult;
        if (input.OutputFileHandling == "original")
        {
            var targetContent = content.Target();
            streamResult = targetContent.Serialize().ToStream();
        }
        else
        {
            streamResult = content.Serialize().ToStream();
        }

        var finalFile = await fileManagementClient.UploadAsync(streamResult, MediaTypes.Xliff, content.XliffFileName);

        result.File = finalFile;
        result.TotalSegmentsProcessed = processedSegmentsCount;
        result.TotalSegmentsFinalized = finalizedSegmentsCount;
        result.TotalSegmentsUnderThreshhold = riskySegmentsCount;
        result.AverageMetric = processedSegmentsCount > 0 ? (totalScore / processedSegmentsCount) : 0f;
        result.PercentageSegmentsUnderThreshhold =
            processedSegmentsCount > 0 ? ((float)riskySegmentsCount / processedSegmentsCount) : 0f;

        return result;
    }

    private record ReviewBatchItem(Unit Unit, Segment Segment, float? Score, float RawScore);
    static UsageDto MapUsage(object usageObj)
    {
        if (usageObj is UsageDto u) return u;
        var json = JsonConvert.SerializeObject(usageObj);
        return JsonConvert.DeserializeObject<UsageDto>(json) ?? new UsageDto();
    }
}
