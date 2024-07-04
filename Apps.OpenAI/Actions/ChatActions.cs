using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using System.Xml.Linq;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Models;
using System.Text.RegularExpressions;
using MoreLinq;
using Apps.OpenAI.Utils.Xliff;
using Blackbird.Xliff.Utils.Extensions;

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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

        if (input.Image != null) model = "gpt-4-vision-preview";

        var messages = await GenerateChatMessages(input, glossary);
        var completeMessage = string.Empty;
        var usage = new UsageDto();

        while (true)
        {
            var jsonBody = new
            {
                model,
                Messages = messages,
                max_tokens = input.MaximumTokens,
                top_p = input.TopP ?? 1,
                presence_penalty = input.PresencePenalty ?? 0,
                frequency_penalty = input.FrequencyPenalty ?? 0,
                temperature = input.Temperature ?? 1
            };

            var jsonBodySerialized = JsonConvert.SerializeObject(jsonBody, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
            request.AddJsonBody(jsonBodySerialized);

            var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
            completeMessage += response.Choices.First().Message.Content;

            usage += response.Usage;

            if (response.Choices.First().FinishReason != "length")
            {
                break;
            }

            messages.Add(new ChatMessageDto(MessageRoles.Assistant, response.Choices.First().Message.Content));
            messages.Add(new ChatMessageDto(MessageRoles.User, "Continue your latest message, it was too long."));
        }

        return new()
        {
            Message = completeMessage,
            SystemPrompt = messages.Where(x => x.GetType() == typeof(ChatMessageDto) && x.Role == MessageRoles.System)
                .Select(x => x.Content).FirstOrDefault() ?? string.Empty,
            UserPrompt = messages.Where(x => x.GetType() == typeof(ChatMessageDto) && x.Role == MessageRoles.User)
                .Select(x => x.Content).FirstOrDefault() ?? string.Empty,
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

    private async Task<List<dynamic>> GenerateChatMessages(ChatRequest input, GlossaryRequest? request)
    {
        var messages = new List<dynamic>();

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
            var glossaryPromptPart = await GetGlossaryPromptPart(request.Glossary, input.Message);
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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

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

            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, content);
            if (glossaryPromptPart != null) prompt += (glossaryAddition + glossaryPromptPart);
        }

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            messages = new List<ChatMessageDto> { new(MessageRoles.System, prompt), new(MessageRoles.User, content) },
            max_tokens = input.MaximumTokens ?? 500,
            top_p = input.TopP ?? 1,
            presence_penalty = input.PresencePenalty ?? 0,
            frequency_penalty = input.FrequencyPenalty ?? 0,
            temperature = input.Temperature ?? 1
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";
        var (messages, info) = BlackbirdPromptParser.ParseBlackbirdPrompt(input.Prompt);

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = messages,
            max_tokens = input.MaximumTokens ?? 4096,
            temperature = input.Temperature ?? 0.5,
            response_format = info?.FileFormat is not null
                ? new { type = BlackbirdPromptParser.ParseFileFormat(info.FileFormat) }
                : null,
            top_p = input.TopP ?? 1,
            presence_penalty = input.PresencePenalty ?? 0,
            frequency_penalty = input.FrequencyPenalty ?? 0,
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.SourceText);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto>
                { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) }
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.SourceText);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto>
                { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) },
            max_tokens = input.MaximumTokens ?? 4096,
            temperature = input.Temperature ?? 0.5
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.SourceText);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto>
                { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) },
            max_tokens = input.MaximumTokens ?? 4096,
            temperature = input.Temperature ?? 0.5
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.SourceText);
            if (glossaryPromptPart != null) userPrompt += glossaryPromptPart;
        }

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto>
                { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) },
            temperature = input.Temperature ?? 0.5,
            response_format = new { type = "json_object" },
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

        var systemPrompt = $"Extract and list all the subject matter terminologies and proper nouns from the text " +
                           $"inputted by the user. Extract words and phrases, instead of sentences. For each term, " +
                           $"provide a terminology entry for the connected language codes: {string.Join(", ", input.Languages)}. Extract words and phrases, instead of sentences. " +
                           $"Return a JSON of the following structure: {{\"result\": [{{{string.Join(", ", input.Languages.Select(x => $"\"{x}\": \"\""))}}}].";

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto>
                { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, input.Content) },
            temperature = input.Temperature ?? 0.5,
            response_format = new { type = "json_object" },
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
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
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

        var systemPrompt = "You are a text localizer. Localize the provided text for the specified locale while " +
                           "preserving the original text structure. Respond with localized text.";

        var userPrompt = @$"
                    Original text: {input.Text}
                    Locale: {input.Locale} 
                
                    ";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary, input.Text);
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

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto>
                { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) },
            max_tokens = input.MaximumTokens ?? 4096,
            temperature = 0.1f
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
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
        var xliffDocument = await LoadAndParseXliffDocument(input.File);
        if (xliffDocument.TranslationUnits.Count == 0)
        {
            return new TranslateXliffResponse { File = input.File, Usage = new UsageDto() };
        }

        var model = modelIdentifier.ModelId ?? "gpt-4o-2024-05-13";
        string systemPrompt = GetSystemPrompt(string.IsNullOrEmpty(prompt));
        var list = xliffDocument.TranslationUnits.Select(x => x.Source).ToList();

        var (translatedTexts, usage) = await GetTranslations(prompt, xliffDocument, model, systemPrompt, list,
            bucketSize ?? 15,
            glossary.Glossary);

        var updatedDocument =
            UpdateXliffDocumentWithTranslations(xliffDocument, translatedTexts, input.UpdateLockedSegments ?? false);
        var fileReference = await UploadUpdatedDocument(updatedDocument, input.File);
        return new TranslateXliffResponse { File = fileReference, Usage = usage };
    }

    [Action("Get Quality Scores for XLIFF file",
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
        var xliffDocument = await LoadAndParseXliffDocument(input.File);
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";
        string criteriaPrompt = string.IsNullOrEmpty(prompt)
            ? "accuracy, fluency, consistency, style, grammar and spelling"
            : prompt;
        var results = new Dictionary<string, float>();
        var batches = xliffDocument.TranslationUnits.Batch((int)bucketSize);
        var src = input.SourceLanguage ?? xliffDocument.SourceLanguage;
        var tgt = input.TargetLanguage ?? xliffDocument.TargetLanguage;

        var usage = new UsageDto();

        foreach (var batch in batches)
        {
            string userPrompt =
                $"Your input is going to be a group of sentences in {src} and their translation into {tgt}. " +
                "Only provide as output the ID of the sentence and the score number as a comma separated array of tuples. " +
                $"Place the tuples in a same line and separate them using semicolons, example for two assessments: 2,7;32,5. The score number is a score from 1 to 10 assessing the quality of the translation, considering the following criteria: {criteriaPrompt}. Sentences: ";
            foreach (var tu in batch)
            {
                userPrompt += $" {tu.Id} {tu.Source} {tu.Target}";
            }

            var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
            request.AddJsonBody(new
            {
                model,
                messages = new List<ChatMessageDto>
                {
                    new(MessageRoles.System,
                        "You are a linguistic expert that should process the following texts accoring to the given instructions"),
                    new(MessageRoles.User, userPrompt)
                },
                max_tokens = 4096,
                temperature = 0.1f
            });

            var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
            usage += response.Usage;
            var result = response.Choices.First().Message.Content;
            foreach (var r in result.Split(";"))
            {
                var split = r.Split(",");
                results.Add(split[0], float.Parse(split[1]));
            }
        }

        var file = await FileManagementClient.DownloadAsync(input.File);
        string fileContent;
        Encoding encoding;
        using (var inFileStream = new StreamReader(file, true))
        {
            encoding = inFileStream.CurrentEncoding;
            fileContent = inFileStream.ReadToEnd();
        }

        foreach (var r in results)
        {
            fileContent = Regex.Replace(fileContent, @"(<trans-unit id=""" + r.Key + @""")",
                @"${1} extradata=""" + r.Value + @"""");
        }

        if (input.Threshold != null && input.Condition != null && input.State != null)
        {
            var filteredTUs = new List<string>();
            switch (input.Condition)
            {
                case ">":
                    filteredTUs = results.Where(x => x.Value > input.Threshold).Select(x => x.Key).ToList();
                    break;
                case ">=":
                    filteredTUs = results.Where(x => x.Value >= input.Threshold).Select(x => x.Key).ToList();
                    break;
                case "=":
                    filteredTUs = results.Where(x => x.Value == input.Threshold).Select(x => x.Key).ToList();
                    break;
                case "<":
                    filteredTUs = results.Where(x => x.Value < input.Threshold).Select(x => x.Key).ToList();
                    break;
                case "<=":
                    filteredTUs = results.Where(x => x.Value <= input.Threshold).Select(x => x.Key).ToList();
                    break;
            }

            fileContent = UpdateTargetState(fileContent, input.State, filteredTUs);
        }

        return new ScoreXliffResponse
        {
            AverageScore = results.Average(x => x.Value),
            File = await FileManagementClient.UploadAsync(new MemoryStream(encoding.GetBytes(fileContent)),
                MediaTypeNames.Text.Xml, input.File.Name),
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
        var xliffDocument = await LoadAndParseXliffDocument(input.File);
        var model = modelIdentifier.ModelId ?? "gpt-4o";
        var results = new List<string>();
        var batches = xliffDocument.TranslationUnits.Batch((int)bucketSize);
        var src = input.SourceLanguage ?? xliffDocument.SourceLanguage;
        var tgt = input.TargetLanguage ?? xliffDocument.TargetLanguage;
        var usage = new UsageDto();

        foreach (var batch in batches)
        {
            string? glossaryPrompt = null;
            if (glossary?.Glossary != null)
            {
                var glossaryPromptPart =
                    await GetGlossaryPromptPart(glossary.Glossary, string.Join(';', batch.Select(x => x.Source)));
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

            var maxId = batch.Max(x => int.Parse(x.Id));
            string userPrompt =
                $"Your input consists of a group of sentences in {src} as the source language and their translations into {tgt}. " +
                "Review the target text and respond with edits to the target text as necessary. If no edits are required, respond with the target text. " +
                "Your reply must include only the list of target texts (updated or unmodified) in the same order as received, each enclosed in curly brackets, and preceded by their respective IDs in the format [ID:X]{target}. " +
                "Example: [ID:1]{target1},[ID:2]{target2},[ID:3]{target3}. If you encounter XML tags in the source text, these tags should also be present in the target text in the same position. If tags are missing in the target, add them. " +
                "Important: Each translation unit consists of a source text and its corresponding target text. For EACH translation unit, you must return exactly one record. " +
                "Respond with the same number of post-edited texts as received. This is crucial for programmatically matching each source to your response. " +
                $"Note that the maximum ID sent to you is {maxId}. If you return an ID greater than this, it indicates a mistake because each translation unit should match exactly to each of your records. " +
                $"{prompt ?? ""}. {glossaryPrompt ?? ""} " +
                $"Sentences: \n";

            foreach (var tu in batch)
            {
                userPrompt += $"ID: {tu.Id}; Source text: {tu.Source}; Target Text: {tu.Target}\n";
            }

            var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
            request.AddJsonBody(new
            {
                model,
                messages = new List<ChatMessageDto>
                {
                    new(MessageRoles.System,
                        "You are a linguistic expert that should process the following texts according to the given instructions"),
                    new(MessageRoles.User, userPrompt)
                },
                max_tokens = 4096,
                temperature = 0.1f
            });

            var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
            usage += response.Usage;
            var result = response.Choices.First().Message.Content;

            var matches = Regex.Matches(result, @"\[ID:\d+\]{([^}]+)}").Select(x => x.Groups[1].Value).ToList();
            if (matches.Count != batch.Length)
            {
                throw new Exception("OpenAI returned an inappropriate response. " +
                                    "The number of post-edited texts does not match the number of source texts. " +
                                    "Probably there is a duplication or a missing text in a translation unit. " +
                                    "Try changing the model or bucket size (to lower values) or add retries to this action.");
            }

            results.AddRange(matches);
        }

        var updatedDocument =
            UpdateXliffDocumentWithTranslations(xliffDocument, results.ToArray(),
                input.PostEditLockedSegments ?? false);
        var fileReference = await UploadUpdatedDocument(updatedDocument, input.File);
        return new TranslateXliffResponse { File = fileReference, Usage = usage, };
    }

    private string UpdateTargetState(string fileContent, string state, List<string> filteredTUs)
    {
        var tus = Regex.Matches(fileContent, @"<trans-unit[\s\S]+?</trans-unit>").Select(x => x.Value);
        foreach (var tu in tus.Where(x =>
                     filteredTUs.Any(y => y == Regex.Match(x, @"<trans-unit id=""(\d+)""").Groups[1].Value)))
        {
            string transformedTU = Regex.IsMatch(tu, @"<target(.*?)state=""(.*?)""(.*?)>")
                ? Regex.Replace(tu, @"<target(.*?state="")(.*?)("".*?)>", @"<target${1}" + state + "${3}>")
                : Regex.Replace(tu, "<target", @"<target state=""" + state + @"""");
            fileContent = Regex.Replace(fileContent, Regex.Escape(tu), transformedTU);
        }

        return fileContent;
    }

    [Action("Get localizable content from image", Description = "Retrieve localizable content from image.")]
    public async Task<ChatResponse> GetLocalizableContentFromImage(
        [ActionParameter] GetLocalizableContentFromImageRequest input)
    {
        var prompt = "Your objective is to conduct optical character recognition (OCR) to identify and extract any " +
                     "localizable content present in the image. Respond with the text found in the image, if any. " +
                     "If no localizable content is detected, provide an empty response.";

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        var fileStream = await FileManagementClient.DownloadAsync(input.Image);
        var fileBytes = await fileStream.GetByteData();
        var jsonBody = new
        {
            model = "gpt-4-vision-preview",
            messages = new List<ChatImageMessageDto>
            {
                new(MessageRoles.User, new List<ChatImageMessageContentDto>
                {
                    new ChatImageMessageTextContentDto("text", prompt),
                    new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                        $"data:{input.Image.ContentType};base64,{Convert.ToBase64String(fileBytes)}"))
                })
            },
            max_tokens = input.MaximumTokens ?? 1000,
            top_p = input.TopP ?? 1,
            presence_penalty = input.PresencePenalty ?? 0,
            frequency_penalty = input.FrequencyPenalty ?? 0,
            temperature = input.Temperature ?? 1
        };
        var jsonBodySerialized = JsonConvert.SerializeObject(jsonBody, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        request.AddJsonBody(jsonBodySerialized);

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
        return new()
        {
            SystemPrompt = prompt,
            UserPrompt = "",
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }

    private async Task<string?> GetGlossaryPromptPart(FileReference glossary, string sourceContent)
    {
        var glossaryStream = await FileManagementClient.DownloadAsync(glossary);
        var blackbirdGlossary = await glossaryStream.ConvertFromTbx();

        var glossaryPromptPart = new StringBuilder();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine("Glossary entries (each entry includes terms in different language. Each " +
                                      "language may have a few synonymous variations which are separated by ;;):");

        var entriesIncluded = false;
        foreach (var entry in blackbirdGlossary.ConceptEntries)
        {
            var allTerms = entry.LanguageSections.SelectMany(x => x.Terms.Select(y => y.Term));
            if (!allTerms.Any(x => Regex.IsMatch(sourceContent, $@"\b{x}\b", RegexOptions.IgnoreCase))) continue;
            entriesIncluded = true;

            glossaryPromptPart.AppendLine();
            glossaryPromptPart.AppendLine("\tEntry:");

            foreach (var section in entry.LanguageSections)
            {
                glossaryPromptPart.AppendLine(
                    $"\t\t{section.LanguageCode}: {string.Join(";; ", section.Terms.Select(term => term.Term))}");
            }
        }

        return entriesIncluded ? glossaryPromptPart.ToString() : null;
    }

    #endregion

    private async Task<XliffDocument> LoadAndParseXliffDocument(FileReference inputFile)
    {
        var stream = await FileManagementClient.DownloadAsync(inputFile);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream.ToXliffDocument(new XliffConfig
            { RemoveWhitespaces = true, CopyAttributes = true, IncludeInlineTags = true });
    }

    private async Task<(string[], UsageDto)> GetTranslations(string prompt, XliffDocument xliffDocument, string model,
        string systemPrompt, List<string> sourceTexts, int bucketSize, FileReference? glossary)
    {
        List<string> allTranslatedTexts = new List<string>();

        int numberOfBuckets = (int)Math.Ceiling(sourceTexts.Count / (double)bucketSize);

        var usageDto = new UsageDto();
        for (int i = 0; i < numberOfBuckets; i++)
        {
            var bucketIndexOffset = i * bucketSize;
            var bucketSourceTexts = sourceTexts
                .Skip(bucketIndexOffset)
                .Take(bucketSize)
                .Select((text, index) => "{ID:" + $"{bucketIndexOffset + index}" + "}" + $"{text}")
                .ToList();

            string json = JsonConvert.SerializeObject(bucketSourceTexts);

            var userPrompt = GetUserPrompt(prompt, xliffDocument, json);

            if (glossary != null)
            {
                var glossaryPromptPart = await GetGlossaryPromptPart(glossary, json);
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

            var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
            request.AddJsonBody(new
            {
                model,
                messages = new List<ChatMessageDto>
                {
                    new(MessageRoles.System, systemPrompt),
                    new(MessageRoles.User, userPrompt)
                },
                max_tokens = 4096,
                temperature = 0.1f
            });

            var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);

            usageDto += response.Usage;
            var translatedText = response.Choices.First().Message.Content.Trim()
                .Replace("```", string.Empty).Replace("json", string.Empty);

            try
            {
                var result = JsonConvert.DeserializeObject<string[]>(translatedText)
                    .Select(t =>
                    {
                        int idEndIndex = t.IndexOf('}') + 1;
                        return idEndIndex < t.Length ? t.Substring(idEndIndex) : string.Empty;
                    })
                    .ToArray();

                if (result.Length != bucketSourceTexts.Count)
                {
                    throw new InvalidOperationException(
                        "OpenAI returned inappropriate response. " +
                        "The number of translated texts does not match the number of source texts. " +
                        "Probably there is a duplication or a missing text in translation unit. " +
                        "Try change model or bucket size (to lower values) or add retries to this action.");
                }

                allTranslatedTexts.AddRange(result);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Failed to parse the translated text in bucket {i + 1}. Exception message: {e.Message}; Exception type: {e.GetType()}");
            }
        }

        return (allTranslatedTexts.ToArray(), usageDto);
    }

    private async Task<FileReference> UploadUpdatedDocument(XliffDocument xliffDocument, FileReference originalFile)
    {
        var outputMemoryStream = originalFile.Name.EndsWith("mqxliff")
            ? xliffDocument.ToStream(null, false, keepSingleAmpersands: true)
            : xliffDocument.ToStream(null, true, keepSingleAmpersands: true);

        string contentType = originalFile.ContentType ?? "application/xml";
        return await FileManagementClient.UploadAsync(outputMemoryStream, contentType, originalFile.Name);
    }

    private XliffDocument UpdateXliffDocumentWithTranslations(XliffDocument xliffDocument, string[] translatedTexts,
        bool updateLockedSegments)
    {
        var updatedUnits = xliffDocument.TranslationUnits.Zip(translatedTexts, (unit, translation) =>
        {
            if (updateLockedSegments == false && unit.Attributes is not null &&
                unit.Attributes.Any(x => x.Key == "locked" && x.Value == "locked"))
            {
                unit.Target = unit.Target;
            }
            else
            {
                unit.Target = translation;
            }

            return unit;
        }).ToList();

        var xDoc = xliffDocument.UpdateTranslationUnits(updatedUnits);
        var stream = new MemoryStream();
        xDoc.Save(stream);
        stream.Position = 0;

        return stream.ToXliffDocument(new XliffConfig
            { RemoveWhitespaces = true, CopyAttributes = true, IncludeInlineTags = true });
    }

    private string GetSystemPrompt(bool translator)
    {
        string prompt;
        if (translator)
        {
            prompt =
                "You are tasked with localizing the provided text. Consider cultural nuances, idiomatic expressions, " +
                "and locale-specific references to make the text feel natural in the target language. " +
                "Ensure the structure of the original text is preserved. Respond with the localized text.";
        }
        else
        {
            prompt =
                "You will be given a list of texts. Each text needs to be processed according to specific instructions " +
                "that will follow. " +
                "The goal is to adapt, modify, or translate these texts as required by the provided instructions. " +
                "Prepare to process each text accordingly and provide the output as instructed.";
        }

        prompt +=
            "Please note that each text is considered as an individual item for translation. Even if there are entries " +
            "that are identical or similar, each one should be processed separately. This is crucial because the output " +
            "should be an array with the same number of elements as the input. This array will be used programmatically, " +
            "so maintaining the same element count is essential.";

        return prompt;
    }

    string GetUserPrompt(string prompt, XliffDocument xliffDocument, string json)
    {
        string instruction = string.IsNullOrEmpty(prompt)
            ? $"Translate the following texts from {xliffDocument.SourceLanguage} to {xliffDocument.TargetLanguage}."
            : $"Process the following texts as per the custom instructions: {prompt}. The source language is {xliffDocument.SourceLanguage} and the target language is {xliffDocument.TargetLanguage}. This information might be useful for the custom instructions.";

        return
            $"Please provide a translation for each individual text, even if similar texts have been provided more than once. " +
            $"{instruction} Return the outputs as a serialized JSON array of strings without additional formatting " +
            $"(it is crucial because your response will be deserialized programmatically. Please ensure that your response is formatted correctly to avoid any deserialization issues). " +
            $"Original texts (in serialized array format): {json}";
    }
}