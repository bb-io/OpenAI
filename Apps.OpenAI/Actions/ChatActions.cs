using System;
using System.Collections.Generic;
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
using TiktokenSharp;
using Blackbird.Applications.Sdk.Glossaries.Utils.Dtos;
using System.Net.Mime;
using System.Text.Json.Nodes;

namespace Apps.OpenAI.Actions;

[ActionList]
public class ChatActions : BaseActions
{
    public ChatActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) 
        : base(invocationContext, fileManagementClient)
    {
    }

    #region Default chat action without prompt

    [Action("Chat", Description = "Gives a response given a chat message")]
    public async Task<ChatResponse> ChatMessageRequest([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ChatRequest input)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

        if (input.Image != null) model = "gpt-4-visual-preview";

        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);

        var jsonBody = new
        {
            model,
            Messages = await GenerateChatMessages(input),
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

        request.AddJsonBody(jsonBodySerialized);

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
        return new()
        {
            Message = response.Choices.First().Message.Content
        };
    }

    private async Task<List<dynamic>> GenerateChatMessages(ChatRequest input)
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
            messages.Add(new ChatMessageDto(MessageRoles.User, input.Message));
        }

        return messages;
    }

    #endregion

    #region Repurposing actions

    [Action("Summarize content", Description = "Summarizes content for different target audiences, languages, tone of voices and platforms")]
    public async Task<RepurposeResponse> CreateSummary([ActionParameter] TextChatModelIdentifier modelIdentifier,
       [ActionParameter][Display("Original content")] string content, [ActionParameter] RepurposeRequest input, [ActionParameter] GlossaryRequest glossary) =>
        await HandleRepurposeRequest("You are a text summarizer. Generate a summary of the message of the user. Be very brief, concise and comprehensive", modelIdentifier, content, input, glossary);    

    [Action("Repurpose content", Description = "Repurpose content for different target audiences, languages, tone of voices and platforms")]
    public async Task<RepurposeResponse> RepurposeContent([ActionParameter] TextChatModelIdentifier modelIdentifier,
    [ActionParameter][Display("Original content")] string content, [ActionParameter] RepurposeRequest input, [ActionParameter] GlossaryRequest glossary) =>
        await HandleRepurposeRequest("Repurpose the content of the message of the user", modelIdentifier, content, input, glossary);

    private async Task<RepurposeResponse> HandleRepurposeRequest(string initialPrompt, TextChatModelIdentifier modelIdentifier, string content, RepurposeRequest input, GlossaryRequest glossary)
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
            prompt += " Enhance the target text by incorporating relevant terms from our glossary where applicable. " +
                            "Ensure that the translation aligns with the glossary entries for the respective languages. " +
                            "If a term has variations or synonyms, consider them and choose the most appropriate " +
                            "translation to maintain consistency and precision. ";

            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary);
            prompt += glossaryPromptPart;
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
            Response = response.Choices.First().Message.Content
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
            Message = response.Choices.First().Message.Content
        };
    }

    #endregion

    #region Translation-related actions

    [Action("Post-edit MT", Description = "Review MT translated text and generate a post-edited version")]
    public async Task<EditResponse> PostEditRequest([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] PostEditRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

        var systemPrompt = "You are receiving a source text that was translated by NMT into target text. Review the " +
                           "target text and respond with edits of the target text as necessary. If no edits required, " +
                           "respond with target text.";

        if (glossary.Glossary != null)
            systemPrompt += " Enhance the target text by incorporating relevant terms from our glossary where applicable. " +
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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary);
            userPrompt += glossaryPromptPart;
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
            EditText = response.Choices.First().Message.Content
        };
    }

    [Action("Get translation issues",
        Description = "Review text translation and generate a comment with the issue description")]
    public async Task<ChatResponse> GetTranslationIssues([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] GetTranslationIssuesRequest input, [ActionParameter] GlossaryRequest glossary)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4-turbo-preview";

        var systemPrompt =
            $"You are receiving a source text {(input.SourceLanguage != null ? $"written in {input.SourceLanguage} " : "")}" +
            $"that was translated by NMT into target text {(input.TargetLanguage != null ? $"written in {input.TargetLanguage}" : "")}. " +
            "Evaluate the target text for grammatical errors, language structure issues, and overall linguistic coherence, " +
            "including them in the issues description. Respond with the issues description.";

        if (glossary.Glossary != null)
            systemPrompt += " Ensure that the translation aligns with the glossary entries provided for the respective " +
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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary);
            userPrompt += glossaryPromptPart;
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
            Message = response.Choices.First().Message.Content
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
            systemPrompt += " Use the provided glossary entries for the respective languages. If there are discrepancies " +
                            "between the translation and glossary, note them in the 'Terminology' part of the report, " +
                            "along with terminology problems not related to the glossary.";
        
        if (input.AdditionalPrompt != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalPrompt}";

        var userPrompt =
            $"{(input.SourceLanguage != null ? $"The {input.SourceLanguage} " : "")}\"{input.SourceText}\" was translated as \"{input.TargetText}\"{(input.TargetLanguage != null ? $" into {input.TargetLanguage}" : "")}.{(input.TargetAudience != null ? $" The target audience is {input.TargetAudience}" : "")}";

        if (glossary.Glossary != null)
        {
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary);
            userPrompt += glossaryPromptPart;
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
            Message = response.Choices.First().Message.Content
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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary);
            userPrompt += glossaryPromptPart;
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
            return JsonConvert.DeserializeObject<MqmAnalysis>(response.Choices.First().Message.Content);
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
            throw new Exception("Something went wrong parsing the output from OpenAI, most likely due to a hallucination!");
        }

        var conceptEntries = new List<GlossaryConceptEntry>();
        int counter = 0;
        foreach (var item in items)
        {
            var languageSections = item.Select(x => new GlossaryLanguageSection(x.Key, new List<GlossaryTermSection> { new GlossaryTermSection(x.Value) })).ToList();

            conceptEntries.Add(new GlossaryConceptEntry(counter.ToString(), languageSections));
            ++counter;
        }
        var blackbirdGlossary = new Glossary(conceptEntries);

        var name = input.Name ?? "New glossary";
        blackbirdGlossary.Title = name;
        using var stream = blackbirdGlossary.ConvertToTBX();
        return new GlossaryResponse() { Glossary = await FileManagementClient.UploadAsync(stream, MediaTypeNames.Application.Xml, $"{name}.tbx") };
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
            var glossaryPromptPart = await GetGlossaryPromptPart(glossary.Glossary);
            userPrompt += "\nEnhance the localized text by incorporating relevant terms from our glossary where applicable. " +
                          "If you encounter terms from the glossary in the text, ensure that the localized text aligns " +
                          "with the glossary entries for the respective languages. If a term has variations or synonyms, " +
                          "consider them and choose the most appropriate translation from the glossary to maintain " +
                          $"consistency and precision. {glossaryPromptPart}";
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
            Message = response.Choices.First().Message.Content
        };
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
            Message = response.Choices.First().Message.Content
        };
    }

    private async Task<string> GetGlossaryPromptPart(FileReference glossary)
    {
        var glossaryStream = await FileManagementClient.DownloadAsync(glossary);
        var blackbirdGlossary = await glossaryStream.ConvertFromTBX();

        var glossaryPromptPart = new StringBuilder();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine();
        glossaryPromptPart.AppendLine("Glossary entries (each entry includes terms in different language. Each " +
                                      "language may have a few synonymous variations which are separated by ;;):");

        foreach (var entry in blackbirdGlossary.ConceptEntries)
        {
            glossaryPromptPart.AppendLine();
            glossaryPromptPart.AppendLine("\tEntry:");
                
            foreach (var section in entry.LanguageSections)
            {
                glossaryPromptPart.AppendLine(
                    $"\t\t{section.LanguageCode}: {string.Join(";; ", section.Terms.Select(term => term.Term))}");
            }
        }

        return glossaryPromptPart.ToString();
    }

    #endregion
}