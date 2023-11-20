using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Chat;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using TiktokenSharp;

namespace Apps.OpenAI.Actions;

[ActionList]
public class ChatActions : BaseActions
{
    public ChatActions(InvocationContext invocationContext) : base(invocationContext) { }

    #region Chat actions

    [Action("Generate completion", Description = "Completes the given prompt")]
    public async Task<CompletionResponse> CreateCompletion([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] CompletionRequest input)
    {
        var model = modelIdentifier.ModelId ?? "text-davinci-003";
        
        var request = new OpenAIRequest("/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            prompt = input.Prompt,
            logprobs = 1,
            max_tokens = input.MaximumTokens ?? 100,
            top_p = input.TopP ?? 1,
            presence_penalty = input.PresencePenalty ?? 0,
            frequency_penalty = input.FrequencyPenalty ?? 0,
            temperature = input.Temperature ?? 1
        });

        var response = await Client.ExecuteWithErrorHandling<CompletionDto>(request);
        return new()
        {
            CompletionText = response.Choices.First().Text
        };
    }

    [Action("Create summary", Description = "Summarizes the input text")]
    public async Task<SummaryResponse> CreateSummary([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] SummaryRequest input)
    {
        var model = modelIdentifier.ModelId ?? "text-davinci-003";
        
        var prompt = @$"
                Summarize the following text.

                Text:
                """"""
                {input.Text}
                """"""

                Summary:
            ";

        var request = new OpenAIRequest("/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            prompt,
            logprobs = 1,
            max_tokens = input.MaximumTokens ?? 100,
            top_p = input.TopP ?? 1,
            presence_penalty = input.PresencePenalty ?? 0,
            frequency_penalty = input.FrequencyPenalty ?? 0,
            temperature = input.Temperature ?? 1
        });

        var response = await Client.ExecuteWithErrorHandling<CompletionDto>(request);
        return new()
        {
            Summary = response.Choices.First().Text
        };
    }

    [Action("Generate edit", Description = "Edit the input text given an instruction prompt")]
    public async Task<EditResponse> CreateEdit([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] EditRequest input)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4";
        
        var systemPrompt = "You are a text editor. Given provided input text, edit it following the instruction and " +
                           "respond with the edited text.";
        
        var userPrompt = @$"
                    Input text: {input.InputText}
                    Instruction: {input.Instruction}
                    Edited text:
                    ";
        
        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            messages = new List<ChatMessageDto> 
                { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) },
            max_tokens = input.MaximumTokens,
            top_p = input.TopP ?? 1,
            presence_penalty = input.PresencePenalty ?? 0,
            frequency_penalty = input.FrequencyPenalty ?? 0,
            temperature = input.Temperature ?? 1
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
        return new()
        {
            EditText = response.Choices.First().Message.Content
        };
    }

    [Action("Chat", Description = "Gives a response given a chat message")]
    public async Task<ChatResponse> ChatMessageRequest([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] ChatRequest input)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4";
        
        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto> { new(MessageRoles.User, input.Message) },
            max_tokens = input.MaximumTokens,
            top_p = input.TopP ?? 1,
            presence_penalty = input.PresencePenalty ?? 0,
            frequency_penalty = input.FrequencyPenalty ?? 0,
            temperature = input.Temperature ?? 1
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request);
        return new()
        {
            Message = response.Choices.First().Message.Content
        };
    }

    [Action("Chat with image", Description = "Gives a response given a chat message and image")]
    public async Task<ChatResponse> ChatWithImage([ActionParameter] ChatWithImageRequest input)
    {
        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        var jsonBody = new
        {
            model = "gpt-4-vision-preview",
            messages = new List<ChatImageMessageDto>
            {
                new(MessageRoles.User, new List<ChatImageMessageContentDto>
                {
                    new ChatImageMessageTextContentDto("text", input.Message),
                    new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                        $"data:{input.Image.ContentType};base64,{Convert.ToBase64String(input.Image.Bytes)}"))
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

    [Action("Chat with system prompt", Description = "Gives a response given a chat message and a configurable system prompt")]
    public async Task<ChatResponse> ChatWithSystemMessageRequest([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] SystemChatRequest input)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4";
        
        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto> 
                { new(MessageRoles.System, input.SystemPrompt), new(MessageRoles.User, input.Message) },
            max_tokens = input.MaximumTokens,
            top_p = input.TopP ?? 1,
            presence_penalty = input.PresencePenalty ?? 0,
            frequency_penalty = input.FrequencyPenalty ?? 0,
            temperature = input.Temperature ?? 1
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
    public async Task<EditResponse> PostEditRequest([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] PostEditRequest input)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4";
        
        var systemPrompt = "You are receiving a source text that was translated by NMT into target text. Review the " +
                     "target text and respond with edits of the target text as necessary. If no edits required, " +
                     "respond with target text.";

        if (input.AdditionalPrompt != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalPrompt}";

        var userPrompt = @$"
            Source text: 
            {input.SourceText}

            Target text: 
            {input.TargetText}
        ";
        
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

    [Action("Get translation issues", Description = "Review text translation and generate a comment with the issue description")]
    public async Task<ChatResponse> GetTranslationIssues([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] GetTranslationIssuesRequest input)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4";
        
        var systemPrompt = $"You are receiving a source text {(input.SourceLanguage != null ? $"written in {input.SourceLanguage} " : "")}that was translated by NMT into target text {(input.TargetLanguage != null ? $"written in {input.TargetLanguage}" : "")}. " +
                           "Review the target text and respond with the issue description.";

        if (input.AdditionalPrompt != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalPrompt}";
        
        var userPrompt = @$"
            Source text: 
            {input.SourceText}

            Target text: 
            {input.TargetText}
        ";
        
        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto> 
                { new(MessageRoles.System, systemPrompt), new(MessageRoles.User, userPrompt) },
            max_tokens = input.MaximumTokens ?? 5000,
            temperature = input.Temperature ?? 0.5
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request); 
        return new()
        {
            Message = response.Choices.First().Message.Content
        };
    }

    [Action("Perform LQA Analysis", Description = "Perform an LQA Analysis of the translation. The result will contain a text with issues if any.")]
    public async Task<ChatResponse> GetLqaAnalysis([ActionParameter] GetTranslationIssuesRequest input)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4";

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

        if (input.AdditionalPrompt != null)
            prompt = $"{prompt} {input.AdditionalPrompt}";

        var chatResult = await Client.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(prompt),
                ChatMessage.FromUser($"{(input.SourceLanguage != null ? $"The {input.SourceLanguage} " : "")}\"{input.SourceText}\" was translated as \"{input.TargetText}\"{(input.TargetLanguage != null ? $" into {input.TargetLanguage}" : "")}"),
            },
            Model = model,
            MaxTokens = input.MaximumTokens ?? 5000,
            Temperature = (float?)(input.Temperature ?? 0.5)
        });
        chatResult.ThrowOnError();

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request); 
        return new()
        {
            Message = response.Choices.First().Message.Content
        };
    }

    [Action("Localize text", Description = "Localize the text provided")]
    public async Task<ChatResponse> LocalizeText([ActionParameter] ModelIdentifier modelIdentifier, 
        [ActionParameter] LocalizeTextRequest input)
    {
        var model = modelIdentifier.ModelId ?? "gpt-4";
        var prompt = @$"
                    Original text: {input.Text}
                    Locale: {input.Locale}
                    Localized text:
                    ";
        var tikToken = await TikToken.GetEncodingAsync("cl100k_base");
        var maximumTokensNumber = tikToken.Encode(input.Text).Count + 100;
        
        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        request.AddJsonBody(new
        {
            model,
            Messages = new List<ChatMessageDto> { new(MessageRoles.User, prompt) },
            max_tokens = maximumTokensNumber,
            temperature = 0.1f
        });

        var response = await Client.ExecuteWithErrorHandling<ChatCompletionDto>(request); 
        return new()
        {
            Message = response.Choices.First().Message.Content
        };
    }    
    
    [Action("Get localizable content from image", Description = "Retrieve localizable content from image")]
    public async Task<ChatResponse> GetLocalizableContentFromImage(
        [ActionParameter] GetLocalizableContentFromImageRequest input)
    {
        var prompt = "Your objective is to conduct optical character recognition (OCR) to identify and extract any " +
                     "localizable content present in the image. Respond with the text found in the image, if any. " +
                     "If no localizable content is detected, provide an empty response.";
        
        var request = new OpenAIRequest("/chat/completions", Method.Post, Creds);
        var jsonBody = new
        {
            model = "gpt-4-vision-preview",
            messages = new List<ChatImageMessageDto>
            {
                new(MessageRoles.User, new List<ChatImageMessageContentDto>
                {
                    new ChatImageMessageTextContentDto("text", prompt),
                    new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                        $"data:{input.Image.ContentType};base64,{Convert.ToBase64String(input.Image.Bytes)}"))
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

    #endregion
}