using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Xliff;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Services;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using MoreLinq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Apps.OpenAI.Actions;

[ActionList("Deprecated XLIFF")]
public class DeprecatedXliffActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Process XLIFF file", Description = "Processes each translation unit in the XLIFF. Supports only 1.2 version of XLIFF. Deprected. Use the 'Translate' action")]
    public async Task<ProcessXliffResponse> TranslateXliff([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] TranslateXliffRequest input,
        [ActionParameter, Display("Additional instructions", Description = "Specify the instruction to be applied to each source tag within a translation unit. For example, 'Translate text'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of source texts to be translated at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = 1500)
    {
        var xliffProcessingService = new ProcessXliffService(new XliffService(FileManagementClient), 
            new JsonGlossaryService(FileManagementClient),
            new OpenAICompletionService(UniversalClient), 
            new ResponseDeserializationService(),
            new PromptBuilderService(), 
            FileManagementClient);

        var result = await xliffProcessingService.ProcessXliffAsync(new OpenAiXliffInnerRequest
        {
            ModelId = UniversalClient.GetModel(modelIdentifier.ModelId),
            Prompt = prompt,
            XliffFile = input.File,
            Glossary = glossary.Glossary,
            BucketSize = bucketSize ?? 1500,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            PostEditLockedSegments = input.UpdateLockedSegments ?? false,
            AddMissingTrailingTags = input.AddMissingTrailingTags ?? false,
            FilterGlossary = input.FilterGlossary ?? true,
            NeverFail = input.NeverFail ?? false,
            BatchRetryAttempts = input.BatchRetryAttempts ?? 2,
            MaxTokens = input.MaxTokens,
            DisableTagChecks = input.DisableTagChecks ?? false,
        });

        return new ProcessXliffResponse(result);
    }

    [Action("Get quality scores for XLIFF file",
        Description = "Gets segment and file level quality scores for XLIFF files")]
    public async Task<ScoreXliffResponse> ScoreXLIFF([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ScoreXliffRequest input, 
        [ActionParameter, Display("Prompt", Description = "Add any linguistic criteria for quality evaluation")] string? prompt,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of translation units to be processed at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = 1500)
    {
        var xliffDocument = await DownloadXliffDocumentAsync(input.File);
        var criteriaPrompt = string.IsNullOrEmpty(prompt)
            ? "accuracy, fluency, consistency, style, grammar and spelling"
            : prompt;

        var results = new List<TranslationEntity>();
        var batches = xliffDocument.TranslationUnits.Batch((int)bucketSize);
        var src = input.SourceLanguage ?? xliffDocument.SourceLanguage;
        var tgt = input.TargetLanguage ?? xliffDocument.TargetLanguage;

        var usage = new UsageDto();

        foreach (var batch in batches)
        {
            var userPrompt = PromptBuilder.BuildQualityScorePrompt(src, tgt, criteriaPrompt,
                JsonConvert.SerializeObject(batch.Select(x => new { x.Id, x.Source, x.Target }).ToList()));

            var messages = new List<ChatMessageDto> { new(MessageRoles.System, PromptBuilder.DefaultSystemPrompt), new(MessageRoles.User, userPrompt) };
            var response = await ExecuteChatCompletion(messages, UniversalClient.GetModel(modelIdentifier.ModelId), new BaseChatRequest { Temperature = 0.1f }, ResponseFormats.GetQualityScoreXliffResponseFormat());
            usage += response.Usage;
            
            var choice = response.Choices.First();
            var content = choice.Message.Content;
            if (choice.FinishReason == "length")
            {
                throw new PluginApplicationException($"The response from Open AI is too long and was cut off. " +
                                                     $"To avoid this, try lowering the 'Bucket size' to reduce the length of the response.");
            }

            TryCatchHelper.TryCatch(() =>
                {
                    var entity = JsonConvert.DeserializeObject<TranslationEntities>(content);
                    results.AddRange(entity.Translations);
                }, $"Failed to deserialize the response from OpenAI, try again later. Response: {content}");
        }

        results.ForEach(x =>
        {
            var translationUnit = xliffDocument.TranslationUnits.FirstOrDefault(tu => tu.Id == x.TranslationId);
            if (translationUnit != null)
            {
                var attribute = translationUnit.Attributes.FirstOrDefault(x => x.Key == "extradata");
                if (!string.IsNullOrEmpty(attribute.Key))
                {
                    translationUnit.Attributes.Remove(attribute.Key);
                    translationUnit.Attributes.Add("extradata", x.QualityScore.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    translationUnit.Attributes.Add("extradata", x.QualityScore.ToString(CultureInfo.InvariantCulture));
                }
            }
        });

        if (input.Threshold != null && input.Condition != null && input.State != null)
        {
            using var e1 = input.Threshold.GetEnumerator();
            using var e2 = input.Condition.GetEnumerator();
            using var e3 = input.State.GetEnumerator();

            while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
            {
                var threshold = e1.Current;
                var condition = e2.Current;
                var state = e3.Current;

                var filteredTUs = new List<string>();
                switch (condition)
                {
                    case ">":
                        filteredTUs = results.Where(x => x.QualityScore > threshold).Select(x => x.TranslationId)
                            .ToList();
                        break;
                    case ">=":
                        filteredTUs = results.Where(x => x.QualityScore >= threshold).Select(x => x.TranslationId)
                            .ToList();
                        break;
                    case "=":
                        filteredTUs = results.Where(x => x.QualityScore == threshold).Select(x => x.TranslationId)
                            .ToList();
                        break;
                    case "<":
                        filteredTUs = results.Where(x => x.QualityScore < threshold).Select(x => x.TranslationId)
                            .ToList();
                        break;
                    case "<=":
                        filteredTUs = results.Where(x => x.QualityScore <= threshold).Select(x => x.TranslationId)
                            .ToList();
                        break;
                }

                filteredTUs.ForEach(x =>
                {
                    var translationUnit = xliffDocument.TranslationUnits.FirstOrDefault(tu => tu.Id == x);
                    if (translationUnit != null)
                    {
                        var stateAttribute = translationUnit.TargetAttributes.FirstOrDefault(x => x.Key == "state");
                        if (!string.IsNullOrEmpty(stateAttribute.Key))
                        {
                            translationUnit.TargetAttributes.Remove(stateAttribute.Key);
                            translationUnit.TargetAttributes.Add("state", state);
                        }
                        else
                        {
                            translationUnit.TargetAttributes.Add("state", state);
                        }
                    }
                });
            }
             
        }

        var stream = xliffDocument.ToStream();
        return new ScoreXliffResponse
        {
            AverageScore = results.Average(x => x.QualityScore),
            File = await FileManagementClient.UploadAsync(stream, MediaTypeNames.Text.Xml, input.File.Name),
            Usage = usage,
        };
    }

    [Action("Post-edit XLIFF file",
        Description = "Updates the targets of XLIFF 1.2 files")]
    public async Task<PostEditXliffResponse> PostEditXLIFF([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] PostEditXliffRequest input, 
        [ActionParameter, Display("Additional instructions", Description = "Additional instructions that will be added to the user prompt. Example: 'Be concise, use technical terms and avoid slang'")] string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of translation units to be processed at once. Default value: 1500. (See our documentation for an explanation)")] int? bucketSize = 1500)
    {
        var postEditService = new PostEditService(new XliffService(FileManagementClient), 
            new JsonGlossaryService(FileManagementClient),
            new OpenAICompletionService(new OpenAiUniversalClient(Creds)), 
            new ResponseDeserializationService(),
            new PromptBuilderService(), 
            FileManagementClient);

        var result = await postEditService.PostEditXliffAsync(new OpenAiXliffInnerRequest
        {
            ModelId = UniversalClient.GetModel(modelIdentifier.ModelId),
            Prompt = prompt,
            XliffFile = input.File,
            Glossary = glossary.Glossary,
            BucketSize = bucketSize ?? 1500,
            SourceLanguage = input.SourceLanguage,
            TargetLanguage = input.TargetLanguage,
            PostEditLockedSegments = input.PostEditLockedSegments ?? false,
            ProcessOnlyTargetState = input.ProcessOnlyTargetState,
            AddMissingTrailingTags = input.AddMissingTrailingTags ?? false,
            FilterGlossary = input.FilterGlossary ?? true,
            NeverFail = input.NeverFail ?? true,
            BatchRetryAttempts = input.BatchRetryAttempts ?? 2,
            MaxTokens = input.MaxTokens,
            ModifiedBy = input.ModifiedBy ?? "Blackbird",
            DisableTagChecks = input.DisableTagChecks ?? false,
        });

        return new PostEditXliffResponse(result);
    }

    [Action("Get MQM report from XLIFF",
       Description = "Perform an LQA Analysis of the translated XLIFF file. The result will be in the MQM framework form.")]
    public async Task<ChatResponse> GetLqaAnalysisFromXliff([ActionParameter] TextChatModelIdentifier modelIdentifier,
       [ActionParameter] GetTranslationIssuesXliffRequest input, [ActionParameter] GlossaryRequest glossary)
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
                           "Formatting: use line spacing between each category. The category name should be bold.";

        if (glossary.Glossary != null)
            systemPrompt +=
                " Use the provided glossary entries for the respective languages. If there are discrepancies " +
                "between the translation and glossary, note them in the 'Terminology' part of the report, " +
                "along with terminology problems not related to the glossary.";

        if (input.AdditionalPrompt != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalPrompt}";

        var XLFservice = new XliffService(fileManagementClient);
        var xliffDocument = await XLFservice.LoadXliffDocumentAsync(input.File);

        var userPrompt =
            $"{(input.SourceLanguage != null ? $"The {input.SourceLanguage} " : $"The {xliffDocument.SourceLanguage}: ")}\"{String.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Source))}\" was translated as " +
            $"\"{String.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Target))}\"{(input.TargetLanguage != null ? $" into {input.TargetLanguage}" : $" into {xliffDocument.TargetLanguage}")}." +
            $"{(input.TargetAudience != null ? $" The target audience is {input.TargetAudience}" : "")}";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, String.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Source)), true);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteChatCompletion(messages, UniversalClient.GetModel(modelIdentifier.ModelId));

        return new()
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Get MQM dimension values from XLIFF",
        Description =
            "Perform an LQA Analysis of a translated XLIFF file. The result will be in the MQM framework form. This action " +
            "only returns the scores (between 1 and 10) of each dimension.")]
    public async Task<MqmAnalysis> GetLqaDimensionValuesXLIFF([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] GetTranslationIssuesXliffRequest input, [ActionParameter] GlossaryRequest glossary)
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

        var XLFservice = new XliffService(fileManagementClient);
        var xliffDocument = await XLFservice.LoadXliffDocumentAsync(input.File);

        var userPrompt =
            $"{(input.SourceLanguage != null ? $"The {input.SourceLanguage} " : $"The {xliffDocument.SourceLanguage}: ")}\"{String.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Source))}\" was translated as " +
            $"\"{String.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Target))}\"{(input.TargetLanguage != null ? $" into {input.TargetLanguage}" : $" into {xliffDocument.TargetLanguage}")}." +
            $"{(input.TargetAudience != null ? $" The target audience is {input.TargetAudience}" : "")}";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, String.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Source)), true);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteChatCompletion(messages, UniversalClient.GetModel(modelIdentifier.ModelId), input, new { type = "json_object" });

        try
        {
            var analysis = JsonConvert.DeserializeObject<MqmAnalysis>(response.Choices.First().Message.Content);
            analysis.Usage = response.Usage;
            return analysis;
        }
        catch
        {
            throw new PluginApplicationException(
                "Something went wrong parsing the output from OpenAI, most likely due to a hallucination!");
        }
    }

    [Action("Get translation issues from XLIFF",
        Description = "Review the translated XLIFF file and generate a comment with the issue description")]
    public async Task<ChatResponse> GetTranslationIssuesFromXliff([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] GetTranslationIssuesXliffRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var XLFservice = new XliffService(fileManagementClient);
        var xliffDocument = await XLFservice.LoadXliffDocumentAsync(input.File);

        var systemPrompt =
            $"You are receiving a source text{(input.SourceLanguage != null ? $" written in {input.SourceLanguage} " : $" written in {xliffDocument.SourceLanguage} ")}" +
            $"that was translated by NMT into target text{(input.TargetLanguage != null ? $" written in {input.TargetLanguage}" : $" written in {xliffDocument.TargetLanguage}")}. " +
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
            {string.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Source))}

            Target text: 
            {string.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Target))}
        ";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, string.Join(" ", xliffDocument.TranslationUnits.Select(x => x.Source)), true);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteChatCompletion(messages, UniversalClient.GetModel(modelIdentifier.ModelId));

        return new()
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Apply prompt to XLIFF file (experimental)",
        Description = "Runs prompt for each translation unit in the XLIFF file according to the provided instructions and updates the target text for each unit. For now it supports only 1.2 version and 2.1 of XLIFF. Supports batching where multiple units will be put into a single prompt.")]
    public async Task<PostEditXliffResponse> PromptXLIFF(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] PromptXliffRequest input,
        [ActionParameter, Display("User prompt")] string prompt,
        [ActionParameter, Display("System prompt")] string? systemPrompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter] BaseChatRequest promptRequest,
        [ActionParameter, Display("Bucket size", Description = "Specify the number of translation units to be processed at once. Default value: 1. (See our documentation for an explanation)")]
            int? bucketSize = 1)
    {
        var postEditService = new PostEditService(
            new XliffService(FileManagementClient),
            new JsonGlossaryService(FileManagementClient),
            new OpenAICompletionService(UniversalClient),
            new ResponseDeserializationService(),
            new PromptBuilderService(),
            FileManagementClient);

        var fileExtension = Path.GetExtension(input.File.Name)?.ToLowerInvariant() ?? string.Empty;
        var result = await postEditService.PostEditXliffAsync(new OpenAiXliffInnerRequest
        {
            ModelId = UniversalClient.GetModel(modelIdentifier.ModelId),
            Prompt = prompt,
            SystemPrompt = systemPrompt,
            OverwritePrompts = true,
            XliffFile = input.File,
            Glossary = glossary.Glossary,
            FilterGlossary = input.FilterGlossary ?? true,
            BucketSize = bucketSize ?? 1,
            ProcessOnlyTargetState = input.ProcessOnlyTargetState,
            AddMissingTrailingTags = input.AddMissingTrailingTags ?? false,
            NeverFail = input.NeverFail ?? true,
            BatchRetryAttempts = input.BatchRetryAttempts ?? 2,
            MaxTokens = promptRequest.MaximumTokens,
            DisableTagChecks = input.DisableTagChecks ?? false,
            PostEditLockedSegments = input.UpdateLockedSegments ?? false,
            ModifiedBy = input.ModifiedBy ?? "Blackbird",
            FileExtension = fileExtension
        });

        return new PostEditXliffResponse(result);
    }
}