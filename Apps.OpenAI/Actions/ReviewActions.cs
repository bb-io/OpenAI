using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Review;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Models.Responses.Review;
using Apps.OpenAI.Utils;
using Blackbird.Ai.Files.Models;
using Blackbird.Ai.Summary.Models;
using Blackbird.Ai.Text.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using DocumentFormat.OpenXml.EMMA;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Apps.OpenAI.Actions;

[ActionList("Review")]
public class ReviewActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Get translation issues", Description = "Reviews translated text and outputs issue descriptions.")]
    public async Task<ChatResponse> GetTranslationIssues(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] GetTranslationIssuesRequest input, 
        [ActionParameter] GlossaryRequest glossary)
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
        var response = await ExecuteApiRequestAsync(messages, modelIdentifier.ModelId);

        return new()
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Get MQM dimension values",
        Description = "Performs MQM analysis for translated text and outputs per-dimension scores with a proposed translation.")]
    public async Task<MqmAnalysis> GetLqaDimensionValues(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] GetTranslationIssuesRequest input, 
        [ActionParameter] GlossaryRequest glossary)
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
        var response = await ExecuteApiRequestAsync(messages, modelIdentifier.ModelId, input, new { type = "json_object" });

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
    public async Task<ReviewTextResponse> ReviewText(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ReviewTextRequest input)
    {
        // The request's Max tokens, Temperature, top_p and top_k were never sent, and still are not.
        var client = CreateLlmClient(modelIdentifier.ModelId, new OpenAiLlmSettings());
        var processor = CreateTextProcessor(client, modelIdentifier.ModelId);

        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ReviewAsync(new ReviewTextOptions
        {
            SourceText = input.SourceText,
            TargetText = input.TargetText,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            AdditionalInstructions = input.AdditionalInstructions,
            GlossaryTbx = glossaryFile
        }));

        return new ReviewTextResponse
        {
            // No usable score falls back to 0, as before, so the text is routed to a human.
            Score = (float)(result.Score ?? 0),
            SystemPrompt = result.Content.SystemPrompt ?? string.Empty,
            UserPrompt = LastUserPrompt(result.Content),
            Usage = ToUsageDto(result.Content.Usage, client, modelIdentifier.ModelId)
        };
    }

    [Action("Review code changes", Description = "Reviews pull request code changes and returns structured inline review findings.")]
    public async Task<CodeReviewResponse> ReviewCodeChanges(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] CodeReviewRequest input)
    {
        var systemPrompt = """
                           You are a senior software engineer performing code review on a GitHub pull request.
                           Return only high-signal, actionable findings grounded in the provided changes and context.
                           Only include a finding when you can confidently point to a specific changed file path and line number.
                           Focus on correctness, reliability, security, performance, and maintainability risks.
                           Do not include praise, generic advice, or low-value style nits.
                           If there are no actionable findings, return an empty findings array and a brief summary.
                           Every finding must use side RIGHT because comments will be posted on the changed side of the diff.
                           Keep titles short. Keep bodies specific and explain the concrete risk.
                           Suggestions should be concise and directly applicable.
                           """;

        if (!string.IsNullOrWhiteSpace(input.AdditionalInstructions))
        {
            systemPrompt = $"{systemPrompt}\nAdditional review instructions: {input.AdditionalInstructions}";
        }

        var userPrompt = $"""
                          Pull request title:
                          {input.PullRequestTitle ?? string.Empty}

                          Pull request description:
                          {input.PullRequestDescription ?? string.Empty}

                          Code changes JSON:
                          {input.CodeChangesJson}
                          """;

        if (!string.IsNullOrWhiteSpace(input.PreviousFindingsJson))
        {
            userPrompt += $"""

                           Previous findings JSON:
                           {input.PreviousFindingsJson}
                           """;
        }

        var messages = new List<ChatMessageDto>
        {
            new(MessageRoles.System, systemPrompt),
            new(MessageRoles.User, userPrompt)
        };

        var response = await ExecuteApiRequestAsync(
            messages,
            model: modelIdentifier.ModelId,
            input: input,
            responseFormat: ResponseFormats.GetCodeReviewResponseFormat());

        try
        {
            var parsed = JsonConvert.DeserializeObject<CodeReviewResponse>(response.Choices.First().Message.Content)
                ?? throw new Exception("Parsed response was null");

            foreach (var finding in parsed.Findings)
            {
                finding.Body = FormatGithubCommentBody(finding);
            }

            parsed.FindingsJson = JsonConvert.SerializeObject(parsed.Findings);
            parsed.SystemPrompt = systemPrompt;
            parsed.UserPrompt = userPrompt;
            parsed.Usage = MapUsage(response.Usage);
            return parsed;
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException($"Could not parse the output from OpenAI: {ex.Message}");
        }
    }

    private static string FormatGithubCommentBody(CodeReviewFinding finding)
    {
        return $"""
                **Severity:** {finding.Severity}
                **Category:** {finding.Category}

                **{finding.Title}**

                {finding.Body}

                **Recommendation:** {finding.Suggestion}
                """;
    }

    [BlueprintActionDefinition(BlueprintAction.ReviewFile)]
    [Action("Review", Description = "Reviews translated file content and outputs segment quality scores.")]
    public async Task<ReviewContentResponse> ReviewContent(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ReviewContentRequest input)
    {
        // Inputs are validated before any download or model call.
        var shouldReview = ReviewSegmentFilter.Create(input.ExcludeSegmentStates);

        var threshold = input.Threshold ?? 0.8;
        if (threshold < 0 || threshold > 1)
            throw new PluginMisconfigurationException("Threshold must be in range 0..1.");

        // The model is resolved lazily by the client, so a file with nothing to review never calls the API.
        var client = CreateLlmClient(modelIdentifier.ModelId, new OpenAiLlmSettings());
        var processor = CreateContentProcessor(client, modelIdentifier.ModelId);

        await using var file = await FileManagementClient.DownloadAsync(input.File);
        await using var glossaryFile = await DownloadOptionalAsync(input.Glossary);

        var result = await AiAdapters.Mapped(() => processor.ReviewAsync(new ReviewStreamOptions
        {
            File = file,
            FileName = input.File.Name,
            MediaType = input.File.ContentType,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            OutputFileHandling = input.OutputFileHandling,
            GlossaryTbx = glossaryFile,
            AdditionalInstructions = input.AdditionalInstructions,
            SegmentStates = AllSegmentStates,
            SegmentFilter = shouldReview,
            ProcessLockedSegments = true,
            Threshold = threshold,
            LowScoreThreshold = threshold,
            StateAboveThreshold = SegmentState.Final,
            Summary = SummaryOptions.For(input.DocumentSummary)
        }));

        var review = result.Content.Review!;

        return new ReviewContentResponse
        {
            File = await UploadAsync(result.File),
            TotalSegmentsProcessed = review.ScoredSegments,
            TotalSegmentsFinalized = review.Finalized,
            TotalSegmentsUnderThreshhold = review.UnderThreshold,
            AverageMetric = (float)review.AverageScore,
            PercentageSegmentsUnderThreshhold = (float)(review.PercentageUnderThreshold / 100),
            Usage = ToUsageDto(result.Content.Usage, client, modelIdentifier.ModelId)
        };
    }

    private static readonly IReadOnlySet<SegmentState> AllSegmentStates = Enum.GetValues<SegmentState>().ToHashSet();

    static UsageDto MapUsage(object usageObj)
    {
        if (usageObj is UsageDto u) return u;
        var json = JsonConvert.SerializeObject(usageObj);
        return JsonConvert.DeserializeObject<UsageDto>(json) ?? new UsageDto();
    }
}
