using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using System.Net.Mime;
using Blackbird.Xliff.Utils;
using System.Text.RegularExpressions;
using Apps.OpenAI.Models.Entities;
using MoreLinq;
using Apps.OpenAI.Models.Requests.Xliff;

namespace Apps.OpenAI.Actions;

[ActionList]
public class ChatActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    #region Default chat action without prompt

    [Action("Chat", Description = "Gives a response given a chat message")]
    public async Task<ChatResponse> ChatMessageRequest([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ChatRequest input,
        [ActionParameter] GlossaryRequest glossary)
    {
        if (input.Image != null) modelIdentifier.ModelId = "gpt-4-vision-preview";

        var messages = await GenerateChatMessages(input, glossary);
        var completeMessage = string.Empty;
        var usage = new UsageDto();

        while (true)
        {
            var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, input);
            completeMessage += response.Choices.First().Message.Content;

            usage += response.Usage;

            if (response.Choices.First().FinishReason != "length")
            {
                break;
            }

            messages.Append(new ChatMessageDto(MessageRoles.Assistant, response.Choices.First().Message.Content));
            messages.Append(new ChatMessageDto(MessageRoles.User, "Continue your latest message, it was too long."));
        }

        return new()
        {
            Message = completeMessage,
            SystemPrompt = messages.Where(x => x.GetType() == typeof(ChatMessageDto) && x.Role == MessageRoles.System)
                .Select(x => ((ChatMessageDto)x).Content).FirstOrDefault() ?? string.Empty,
            UserPrompt = messages.Where(x => x.GetType() == typeof(ChatMessageDto) && x.Role == MessageRoles.User)
                .Select(x => ((ChatMessageDto)x).Content).FirstOrDefault() ?? string.Empty,
            Usage = usage,
        };
    }

    // This action may seem redundant but we feel that the optional system prompt in the action above may be too hidden.
    [Action("Chat with system prompt", Description = "Gives a response given a chat message")]
    public async Task<ChatResponse> ChatWithSystemMessageRequest(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ChatRequestWithSystem input,
        [ActionParameter] GlossaryRequest glossary)
    {
        return await ChatMessageRequest(modelIdentifier, new ChatRequest
        {
            SystemPrompt = input.SystemPrompt,
            Message = input.Message,
            MaximumTokens = input.MaximumTokens,
            FrequencyPenalty = input.FrequencyPenalty,
            Image = input.Image,
            Parameters = input.Parameters,
            PresencePenalty = input.PresencePenalty,
            Temperature = input.Temperature,
            TopP = input.TopP
        }, glossary);
    }

    private async Task<ChatCompletionDto> ExecuteChatCompletion(IEnumerable<object> messages, string model = "gpt-4-turbo-preview", BaseChatRequest input = null, object responseFormat = null)
    {
        var jsonBody = new
        {
            model,
            Messages = messages,
            max_tokens = !model.Contains("o1") ? (int?)(input?.MaximumTokens ?? 4096) : null,
            max_completion_tokens = model.Contains("o1") ? (int?)(input?.MaximumTokens ?? 4096) : null,
            top_p = input?.TopP ?? 1,
            presence_penalty = input?.PresencePenalty ?? 0,
            frequency_penalty = input?.FrequencyPenalty ?? 0,
            temperature = input?.Temperature ?? 1,
            response_format = responseFormat,
        };

        var jsonBodySerialized = JsonConvert.SerializeObject(jsonBody, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        });

        var request = new OpenAIRequest("/chat/completions", Method.Post);
        request.AddJsonBody(jsonBodySerialized);

        return await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
    }

    private async Task<IEnumerable<BaseChatMessageDto>> GenerateChatMessages(ChatRequest input, GlossaryRequest? request)
    {
        var messages = new List<BaseChatMessageDto>();

        if (input.SystemPrompt != null)
            messages.Add(new ChatMessageDto(MessageRoles.System, input.SystemPrompt));

        if (input.Image != null)
        {
            var fileStream = await FileManagementClient.DownloadAsync(input.Image);
            var fileBytes = await fileStream.GetByteData();
            if (input.SystemPrompt != null)
                messages.Add(new ChatMessageDto(MessageRoles.System, input.SystemPrompt));
            messages.Add(new ChatImageMessageDto(MessageRoles.User, new List<ChatImageMessageContentDto>
            {
                new ChatImageMessageTextContentDto("text", input.Message),
                new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                    $"data:{input.Image.ContentType};base64,{Convert.ToBase64String(fileBytes)}"))
            }));
        }
        else
        {
            if (input.Parameters != null)
            {
                var stringBuilder = new StringBuilder();
                foreach (var message in input.Parameters)
                {
                    stringBuilder.AppendLine(message);
                }

                var prompt =
                    $"{input.Message}; Parameters that you should use (they can be in json format): {stringBuilder}";
                messages.Add(new ChatMessageDto(MessageRoles.User, prompt));
            }
            else
            {
                messages.Add(new ChatMessageDto(MessageRoles.User, input.Message));
            }
        }

        if (request?.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(request.Glossary, input.Message, true);
            if (glossaryPromptPart != null)
                messages.Add(new ChatMessageDto(MessageRoles.User, $"Glossary: {glossaryPromptPart}"));
        }

        return messages;
    }

    #endregion

    #region Repurposing actions

    [Action("Summarize content",
        Description = "Summarizes content for different target audiences, languages, tone of voices and platforms")]
    public async Task<RepurposeResponse> CreateSummary([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] [Display("Original content")]
        string content, [ActionParameter] RepurposeRequest input, [ActionParameter] GlossaryRequest glossary) =>
        await HandleRepurposeRequest(
            "You are a text summarizer. Generate a summary of the message of the user. Be very brief, concise and comprehensive",
            modelIdentifier, content, input, glossary);

    [Action("Repurpose content",
        Description = "Repurpose content for different target audiences, languages, tone of voices and platforms")]
    public async Task<RepurposeResponse> RepurposeContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] [Display("Original content")]
        string content, [ActionParameter] RepurposeRequest input, [ActionParameter] GlossaryRequest glossary) =>
        await HandleRepurposeRequest("Repurpose the content of the message of the user", modelIdentifier, content,
            input, glossary);

    private async Task<RepurposeResponse> HandleRepurposeRequest(string initialPrompt,
        TextChatModelIdentifier modelIdentifier, string content, RepurposeRequest input, GlossaryRequest glossary)
    {
        var prompt = @$"
                {initialPrompt}. 
                {input.AdditionalPrompt}. 
                {(input.TargetAudience != null ? $"The target audience is {input.TargetAudience}" : string.Empty)}.
                {input.ToneOfVOice}
                {(input.Locale != null ? $"The response should be in {input.Locale}" : string.Empty)}

            ";

        if (glossary.Glossary != null)
        {
            var glossaryAddition =
                " Enhance the target text by incorporating relevant terms from our glossary where applicable. " +
                "Ensure that the translation aligns with the glossary entries for the respective languages. " +
                "If a term has variations or synonyms, consider them and choose the most appropriate " +
                "translation to maintain consistency and precision. ";

            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, content, true);
            if (glossaryPromptPart != null) prompt += (glossaryAddition + glossaryPromptPart);
        }
        var messages = new List<ChatMessageDto> { new(MessageRoles.System, prompt), new(MessageRoles.User, content) };
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, input);

        return new()
        {
            SystemPrompt = prompt,
            Response = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Execute Blackbird prompt", Description = "Execute prompt generated by Blackbird's AI utilities")]
    public async Task<ChatResponse> ExecuteBlackbirdPrompt([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ExecuteBlackbirdPromptRequest input)
    {
        var (messages, info) = BlackbirdPromptParser.ParseBlackbirdPrompt(input.Prompt);

        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, input,
            info?.FileFormat is not null
                ? new { type = BlackbirdPromptParser.ParseFileFormat(info.FileFormat) }
                : null);

        return new()
        {
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    #endregion

    #region Translation-related actions

    [Action("Post-edit MT", Description = "Review MT translated text and generate a post-edited version")]
    public async Task<EditResponse> PostEditRequest([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] PostEditRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var systemPrompt =
            $"You are receiving a source text{(input.SourceLanguage != null ? $" written in {input.SourceLanguage} " : "")}" +
            $"that was translated by NMT into target text{(input.TargetLanguage != null ? $" written in {input.TargetLanguage}" : "")}. " +
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
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId);
        return new()
        {
            UserPrompt = userPrompt,
            SystemPrompt = systemPrompt,
            EditText = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Get translation issues",
        Description = "Review text translation and generate a comment with the issue description")]
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
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId);

        return new()
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Get MQM report",
        Description = "Perform an LQA Analysis of the translation. The result will be in the MQM framework form.")]
    public async Task<ChatResponse> GetLqaAnalysis([ActionParameter] TextChatModelIdentifier modelIdentifier,
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
                           "Formatting: use line spacing between each category. The category name should be bold.";

        if (glossary.Glossary != null)
            systemPrompt +=
                " Use the provided glossary entries for the respective languages. If there are discrepancies " +
                "between the translation and glossary, note them in the 'Terminology' part of the report, " +
                "along with terminology problems not related to the glossary.";

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
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId);

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
            "Perform an LQA Analysis of the translation. The result will be in the MQM framework form. This action " +
            "only returns the scores (between 1 and 10) of each dimension.")]
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
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, input, new { type = "json_object" });

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

    [Action("Extract glossary", Description = "Extract glossary terms from a given text. Use in combination with " +
                                              "other glossary actions.")]
    public async Task<GlossaryResponse> ExtractGlossary([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ExtractGlossaryRequest input)
    {
        var systemPrompt = $"Extract and list all the subject matter terminologies and proper nouns from the text " +
                           $"inputted by the user. Extract words and phrases, instead of sentences. For each term, " +
                           $"provide a terminology entry for the connected language codes: {string.Join(", ", input.Languages)}. Extract words and phrases, instead of sentences. " +
                           $"Return a JSON of the following structure: {{\"result\": [{{{string.Join(", ", input.Languages.Select(x => $"\"{x}\": \"\""))}}}].";

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, input.Content) };
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, input, new { type = "json_object" });

        List<Dictionary<string, string>> items = null;
        try
        {
            items = JsonConvert.DeserializeObject<GlossaryItemWrapper>(response.Choices.First().Message.Content).Result;
        }
        catch
        {
            throw new Exception(
                "Something went wrong parsing the output from OpenAI, most likely due to a hallucination!");
        }

        var conceptEntries = new List<GlossaryConceptEntry>();
        int counter = 0;
        foreach (var item in items)
        {
            var languageSections = item.Select(x =>
                    new GlossaryLanguageSection(x.Key,
                        new List<GlossaryTermSection> { new GlossaryTermSection(x.Value) }))
                .ToList();

            conceptEntries.Add(new GlossaryConceptEntry(counter.ToString(), languageSections));
            ++counter;
        }

        var blackbirdGlossary = new Glossary(conceptEntries);

        var name = input.Name ?? "New glossary";
        blackbirdGlossary.Title = name;
        using var stream = blackbirdGlossary.ConvertToTbx();
        return new GlossaryResponse()
        {
            UserPrompt = input.Content,
            SystemPrompt = systemPrompt,
            Glossary = await FileManagementClient.UploadAsync(stream, MediaTypeNames.Application.Xml, $"{name}.tbx"),
            Usage = response.Usage,
        };
    }

    [Action("Translate text", Description = "Localize the text provided.")]
    public async Task<ChatResponse> LocalizeText([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] LocalizeTextRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var systemPrompt = "You are a text localizer. Localize the provided text for the specified locale while " +
                           "preserving the original text structure. Respond with localized text.";

        var userPrompt = @$"
                    Original text: {input.Text}
                    Locale: {input.Locale} 
                
                    ";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.Text, true);
            if (glossaryPromptPart != null)
            {
                userPrompt +=
                    "\nEnhance the localized text by incorporating relevant terms from our glossary where applicable. " +
                    "If you encounter terms from the glossary in the text, ensure that the localized text aligns " +
                    "with the glossary entries for the respective languages. If a term has variations or synonyms, " +
                    "consider them and choose the most appropriate translation from the glossary to maintain " +
                    $"consistency and precision. {glossaryPromptPart}";
            }
        }

        userPrompt += "Localized text: ";

        var messages = new List<ChatMessageDto> { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) };
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, input);

        return new()
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    [Action("Process XLIFF file",
        Description =
            "Processes each translation unit in the XLIFF file according to the provided instructions (by default it just translates the source tags) and updates the target text for each unit. For now it supports only 1.2 version of XLIFF.")]
    public async Task<TranslateXliffResponse> TranslateXliff([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] TranslateXliffRequest input,
        [ActionParameter,
         Display("Prompt",
             Description =
                 "Specify the instruction to be applied to each source tag within a translation unit. For example, 'Translate text'")]
        string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter,
         Display("Bucket size",
             Description =
                 "Specify the number of source texts to be translated at once. Default value: 1500. (See our documentation for an explanation)")]
        int? bucketSize = 1500)
    {
        var xliffDocument = await DownloadXliffDocumentAsync(input.File);
        var systemPrompt = PromptBuilder.BuildSystemPrompt(string.IsNullOrEmpty(prompt));
        var (translatedTexts, usage) = await ProcessTranslationUnits(prompt, xliffDocument, modelIdentifier.ModelId, systemPrompt,
            bucketSize ?? 1500,
            glossary.Glossary, input.FilterGlossary ?? true);

        translatedTexts.ForEach(x =>
        {
            var translationUnit = xliffDocument.TranslationUnits.FirstOrDefault(tu => tu.Id == x.TranslationId);
            if (translationUnit != null)
            {
                if (input.AddMissingTrailingTags.HasValue && input.AddMissingTrailingTags == true)
                {
                    var sourceContent = translationUnit.Source;
                    var targetContent = translationUnit.Target;

                    var tagPattern = @"<(?<tag>\w+)([^>]*)>(?<content>.*)</\k<tag>>";
                    var sourceMatch = Regex.Match(sourceContent, tagPattern, RegexOptions.Singleline);

                    if (sourceMatch.Success)
                    {
                        var tagName = sourceMatch.Groups["tag"].Value;
                        var tagAttributes = sourceMatch.Groups[2].Value;
                        var openingTag = $"<{tagName}{tagAttributes}>";
                        var closingTag = $"</{tagName}>";

                        if (!targetContent.Contains(openingTag) && !targetContent.Contains(closingTag))
                        {
                            translationUnit.Target = openingTag + targetContent + closingTag;
                        }
                    }
                    else
                    {
                        translationUnit.Target = x.TranslatedText;
                    }
                }
                else
                {
                    translationUnit.Target = x.TranslatedText;
                } 
            }
        });

        var stream = xliffDocument.ToStream();
        var fileReference = await fileManagementClient.UploadAsync(stream, input.File.ContentType, input.File.Name);
        return new TranslateXliffResponse { File = fileReference, Usage = usage };
    }

    [Action("Get quality scores for XLIFF file",
        Description = "Gets segment and file level quality scores for XLIFF files")]
    public async Task<ScoreXliffResponse> ScoreXLIFF([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ScoreXliffRequest input, [ActionParameter,
                                                    Display("Prompt",
                                                        Description =
                                                            "Add any linguistic criteria for quality evaluation")]
        string? prompt,
        [ActionParameter,
         Display("Bucket size",
             Description =
                 "Specify the number of translation units to be processed at once. Default value: 1500. (See our documentation for an explanation)")]
        int? bucketSize = 1500)
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
            var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, new BaseChatRequest { Temperature = 0.1f }, ResponseFormats.GetQualityScoreXliffResponseFormat());
            
            var content = response.Choices.First().Message.Content;
            usage += response.Usage;

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
            var filteredTUs = new List<string>();
            switch (input.Condition)
            {
                case ">":
                    filteredTUs = results.Where(x => x.QualityScore > input.Threshold).Select(x => x.TranslationId)
                        .ToList();
                    break;
                case ">=":
                    filteredTUs = results.Where(x => x.QualityScore >= input.Threshold).Select(x => x.TranslationId)
                        .ToList();
                    break;
                case "=":
                    filteredTUs = results.Where(x => x.QualityScore == input.Threshold).Select(x => x.TranslationId)
                        .ToList();
                    break;
                case "<":
                    filteredTUs = results.Where(x => x.QualityScore < input.Threshold).Select(x => x.TranslationId)
                        .ToList();
                    break;
                case "<=":
                    filteredTUs = results.Where(x => x.QualityScore <= input.Threshold).Select(x => x.TranslationId)
                        .ToList();
                    break;
            }

            filteredTUs.ForEach(x =>
            {
                var translationUnit = xliffDocument.TranslationUnits.FirstOrDefault(tu => tu.Id == x);
                if (translationUnit != null)
                {
                    var stateAttribute = translationUnit.Attributes.FirstOrDefault(x => x.Key == "state");
                    if (!string.IsNullOrEmpty(stateAttribute.Key))
                    {
                        translationUnit.Attributes.Remove(stateAttribute.Key);
                        translationUnit.Attributes.Add("state", input.State);
                    }
                    else
                    {
                        translationUnit.Attributes.Add("state", input.State);
                    }
                }
            });
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
    public async Task<TranslateXliffResponse> PostEditXLIFF([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] PostEditXliffRequest input, [ActionParameter,
                                                       Display("Prompt",
                                                           Description =
                                                               "Additional instructions")]
        string? prompt,
        [ActionParameter] GlossaryRequest glossary,
        [ActionParameter,
         Display("Bucket size",
             Description =
                 "Specify the number of translation units to be processed at once. Default value: 1500. (See our documentation for an explanation)")]
        int? bucketSize = 1500)
    {
        var xliffDocument = await DownloadXliffDocumentAsync(input.File);

        var results = new List<TranslationEntity>();
        var batches = xliffDocument.TranslationUnits.Batch((int)bucketSize);
        var src = input.SourceLanguage ?? xliffDocument.SourceLanguage;
        var tgt = input.TargetLanguage ?? xliffDocument.TargetLanguage;
        var usage = new UsageDto();

        foreach (var batch in batches)
        {
            string? glossaryPrompt = null;

            var filteredBatch = input.PostEditLockedSegments == true
                ? batch 
                : batch.Where(x => !x.IsLocked()).ToArray();
            
            if (glossary?.Glossary != null)
            {
                var glossaryPromptPart =
                    await GetGlossaryPromptPart(glossary.Glossary,
                        string.Join(';', filteredBatch.Select(x => x.Source)) + ";" +
                        string.Join(';', filteredBatch.Select(x => x.Target)), input.FilterGlossary ?? true);
                if (glossaryPromptPart != null)
                {
                    glossaryPrompt +=
                        "Enhance the target text by incorporating relevant terms from our glossary where applicable. " +
                        "Ensure that the translation aligns with the glossary entries for the respective languages. " +
                        "If a term has variations or synonyms, consider them and choose the most appropriate " +
                        "translation to maintain consistency and precision. ";
                    glossaryPrompt += glossaryPromptPart;
                }
            }

            var userPrompt =
                $"Your input consists of sentences in {src} language with their translations into {tgt}. " +
                "Review and edit the translated target text as necessary to ensure it is a correct and accurate translation of the source text. " +
                "If you see XML tags in the source also include them in the target text, don't delete or modify them. " +
                $"{prompt ?? ""} {glossaryPrompt ?? ""} Sentences: \n" +
                JsonConvert.SerializeObject(filteredBatch.Select(x => new { x.Id, x.Source, x.Target }));

            var messages = new List<ChatMessageDto> { new(MessageRoles.System, PromptBuilder.DefaultSystemPrompt), new(MessageRoles.User, userPrompt) };
            var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, new BaseChatRequest { Temperature = 0.1f }, ResponseFormats.GetProcessXliffResponseFormat());

            var content = response.Choices.First().Message.Content;
            usage += response.Usage;

            TryCatchHelper.TryCatch(() =>
                {
                    var deserializedResponse = JsonConvert.DeserializeObject<TranslationEntities>(content);
                    results.AddRange(deserializedResponse.Translations);
                }, $"Failed to deserialize the response from OpenAI, try again later. Response: {content}");
        }

        var dictionary = results.ToDictionary(x => x.TranslationId, x => x.TranslatedText);
        var updatedResults =
            Utils.Xliff.Extensions.CheckTagIssues(xliffDocument.TranslationUnits, dictionary);
        updatedResults.ForEach(x =>
        {
            var translationUnit = xliffDocument.TranslationUnits.FirstOrDefault(tu => tu.Id == x.Key);
            if (translationUnit != null)
            {
                if (input.AddMissingTrailingTags.HasValue && input.AddMissingTrailingTags == true)
                {
                    var sourceContent = translationUnit.Source;
                    
                    var tagPattern = @"<(?<tag>\w+)(?<attributes>[^>]*)>(?<content>.*?)</\k<tag>>";
                    var sourceMatch = Regex.Match(sourceContent, tagPattern, RegexOptions.Singleline);

                    if (sourceMatch.Success)
                    {
                        var tagName = sourceMatch.Groups["tag"].Value;
                        var tagAttributes = sourceMatch.Groups["attributes"].Value;
                        var openingTag = $"<{tagName}{tagAttributes}>";
                        var closingTag = $"</{tagName}>";

                        if (!x.Value.Contains(openingTag) && !x.Value.Contains(closingTag))
                        {
                            translationUnit.Target = openingTag + x.Value + closingTag;
                        }
                        else
                        {
                            translationUnit.Target = x.Value;
                        }
                    }
                    else
                    {
                        translationUnit.Target = x.Value;
                    }
                }
                else
                {
                    translationUnit.Target = x.Value;
                }
            }
        });

        var finalFile =
            await FileManagementClient.UploadAsync(xliffDocument.ToStream(), input.File.ContentType, input.File.Name);
        return new TranslateXliffResponse { File = finalFile, Usage = usage, };
    }

    [Action("Get localizable content from image", Description = "Retrieve localizable content from image.")]
    public async Task<ChatResponse> GetLocalizableContentFromImage(
        [ActionParameter] ImageChatModelIdentifier modelIdentifier,
        [ActionParameter] GetLocalizableContentFromImageRequest input)
    {
        var prompt = "Your objective is to conduct optical character recognition (OCR) to identify and extract any " +
                     "localizable content present in the image. Respond with the text found in the image, if any. " +
                     "If no localizable content is detected, provide an empty response.";

        var fileStream = await FileManagementClient.DownloadAsync(input.Image);
        var fileBytes = await fileStream.GetByteData();
        var messages = new List<ChatImageMessageDto>
            {
                new(MessageRoles.User, new List<ChatImageMessageContentDto>
                {
                    new ChatImageMessageTextContentDto("text", prompt),
                    new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                        $"data:{input.Image.ContentType};base64,{Convert.ToBase64String(fileBytes)}"))
                })
            };
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, input);

        return new()
        {
            SystemPrompt = prompt,
            UserPrompt = "",
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    #endregion
    
    private async Task<(List<TranslationEntity>, UsageDto)> ProcessTranslationUnits(string prompt,
        XliffDocument xliff, string model,
        string systemPrompt, int bucketSize, FileReference? glossary, bool filter)
    {
        var results = new List<TranslationEntity>();
        var batches = xliff.TranslationUnits.Batch(bucketSize);

        var usageDto = new UsageDto();
        foreach (var batch in batches)
        {
            var json = JsonConvert.SerializeObject(batch.Select(x => new
                { x.Id, x.Source }));
            var userPrompt = PromptBuilder.BuildUserPrompt(prompt, xliff.SourceLanguage, xliff.TargetLanguage, json);

            if (glossary != null)
            {
                var glossaryPromptPart = await GetGlossaryPromptPart(glossary, json, filter);
                if (glossaryPromptPart != null)
                {
                    var glossaryPrompt =
                        "Enhance the target text by incorporating relevant terms from our glossary where applicable. " +
                        "Ensure that the translation aligns with the glossary entries for the respective languages. " +
                        "If a term has variations or synonyms, consider them and choose the most appropriate " +
                        "translation to maintain consistency and precision. ";
                    glossaryPrompt += glossaryPromptPart;
                    userPrompt += glossaryPrompt;
                }
            }

            var messages = new List<ChatMessageDto>
                    {
                        new(MessageRoles.System, systemPrompt),
                        new(MessageRoles.User, userPrompt)
                    };
            var response = await ExecuteChatCompletion(messages, model, new BaseChatRequest { Temperature = 0.1f });

            var content = response.Choices.First().Message.Content;
            usageDto += response.Usage;
            TryCatchHelper.TryCatch(() =>
                {
                    var deserializedResponse = JsonConvert.DeserializeObject<TranslationEntities>(content);
                    results.AddRange(deserializedResponse.Translations);
                }, $"Failed to deserialize the response from OpenAI, try again later. Response: {content}");
        }

        return (results, usageDto);
    }
}