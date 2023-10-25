using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Extensions;
using Apps.OpenAI.Invocables;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Chat;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using TiktokenSharp;

namespace Apps.OpenAI.Actions;

[ActionList]
public class ChatActions : OpenAiInvocable
{
    private IOpenAIService Client { get; }

    public ChatActions(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = Creds.CreateOpenAiServiceSdk();
    }

    #region Chat actions

    [Action("Generate completion", Description = "Completes the given prompt")]
    public async Task<CompletionResponse> CreateCompletion([ActionParameter] CompletionRequest input)
    {
        var model = input.Model ?? "text-davinci-003";

        var completionResult = await Client.Completions.CreateCompletion(new CompletionCreateRequest
        {
            Prompt = input.Prompt,
            LogProbs = 1,
            MaxTokens = input.MaximumTokens,
            Model = model,
            TopP = input.TopP,
            PresencePenalty = input.PresencePenalty,
            FrequencyPenalty = input.FrequencyPenalty,
            Temperature = input.Temperature
        });
        completionResult.ThrowOnError();

        return new()
        {
            CompletionText = completionResult.Choices.First().Text
        };
    }

    [Action("Create summary", Description = "Summarizes the input text")]
    public async Task<SummaryResponse> CreateSummary([ActionParameter] SummaryRequest input)
    {
        var model = input.Model ?? "text-davinci-003";
        var prompt = @$"
                Summarize the following text.

                Text:
                """"""
                {input.Text}
                """"""

                Summary:
            ";

        var completionResult = await Client.Completions.CreateCompletion(new CompletionCreateRequest
        {
            Prompt = prompt,
            LogProbs = 1,
            MaxTokens = input.MaximumTokens,
            Model = model,
            TopP = input.TopP,
            PresencePenalty = input.PresencePenalty,
            FrequencyPenalty = input.FrequencyPenalty,
            Temperature = input.Temperature
        });
        completionResult.ThrowOnError();

        return new()
        {
            Summary = completionResult.Choices.First().Text
        };
    }

    [Action("Generate edit", Description = "Edit the input text given an instruction prompt")]
    public async Task<EditResponse> CreateEdit([ActionParameter] EditRequest input)
    {
        var model = input.Model ?? "text-davinci-edit-001";
        var editResult = await Client.Edit.CreateEdit(new EditCreateRequest
        {
            Input = input.InputText,
            Instruction = input.Instruction,
            Model = model,
            Temperature = input.Temperature,
            TopP = input.TopP,
        });
        editResult.ThrowOnError();

        return new()
        {
            EditText = editResult.Choices.First().Text
        };
    }

    [Action("Chat", Description = "Gives a response given a chat message")]
    public async Task<ChatResponse> ChatMessageRequest([ActionParameter] ChatRequest input)
    {
        var model = input.Model ?? "gpt-4";
        var chatResult = await Client.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromUser(input.Message),
            },
            MaxTokens = input.MaximumTokens,
            Model = model,
            TopP = input.TopP,
            PresencePenalty = input.PresencePenalty,
            FrequencyPenalty = input.FrequencyPenalty,
            Temperature = input.Temperature
        });
        chatResult.ThrowOnError();

        return new()
        {
            Message = chatResult.Choices.First().Message.Content
        };
    }

    [Action("Chat with system prompt",
        Description = "Gives a response given a chat message and a configurable system prompt")]
    public async Task<ChatResponse> ChatWithSystemMessageRequest([ActionParameter] SystemChatRequest input)
    {
        var model = input.Model ?? "gpt-4";
        var chatResult = await Client.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(input.SystemPrompt),
                ChatMessage.FromUser(input.Message),
            },
            MaxTokens = input.MaximumTokens,
            Model = model,
            TopP = input.TopP,
            PresencePenalty = input.PresencePenalty,
            FrequencyPenalty = input.FrequencyPenalty,
            Temperature = input.Temperature
        });
        chatResult.ThrowOnError();

        return new()
        {
            Message = chatResult.Choices.First().Message.Content
        };
    }

    #endregion

    #region Translation-related actions

    [Action("Post-edit MT", Description = "Review MT translated text and generate a post-edited version")]
    public async Task<EditResponse> PostEditRequest([ActionParameter] PostEditRequest input)
    {
        var model = input.Model ?? "gpt-4";
        var prompt = "You are receiving a source text that was translated by NMT into target text. Review the " +
                     "target text and respond with edits of the target text as necessary. If no edits required, " +
                     "respond with target text.";

        if (input.AdditionalPrompt != null)
            prompt = $"{prompt} {input.AdditionalPrompt}";

        var chatResult = await Client.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(prompt),
                ChatMessage.FromUser(@$"
                        Source text: 
                        {input.SourceText}

                        Target text: 
                        {input.TargetText}
                    "),
            },
            Model = model
        });
        chatResult.ThrowOnError();

        return new()
        {
            EditText = chatResult.Choices.First().Message.Content
        };
    }

    [Action("Get translation issues",
        Description = "Review text translation and generate a comment with the issue description")]
    public async Task<ChatResponse> GetTranslationIssues([ActionParameter] GetTranslationIssuesRequest input)
    {
        var model = input.Model ?? "gpt-4";

        var prompt = $"You are receiving a source text {(input.SourceLanguage != null ? $"written in {input.SourceLanguage} " : "")}that was translated by NMT into target text {(input.TargetLanguage != null ? $"written in {input.TargetLanguage}" : "")}. " +
                     "Review the target text and respond with the issue description.";

        if (input.AdditionalPrompt != null)
            prompt = $"{prompt} {input.AdditionalPrompt}";

        var chatResult = await Client.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(prompt),
                ChatMessage.FromUser(@$"
                        Source text: 
                        {input.SourceText}

                        Target text: 
                        {input.TargetText}
                    "),
            },
            Model = model,
            MaxTokens = input.MaximumTokens ?? 5000,
            Temperature = (float?)(input.Temperature ?? 0.5)
        });
        chatResult.ThrowOnError();

        return new()
        {
            Message = chatResult.Choices.First().Message.Content
        };
    }

    [Action("Perform LQA Analysis", Description = "Perform an LQA Analysis of the translation. The result will contain a text with issues if any.")]
    public async Task<ChatResponse> GetLqaAnalysis([ActionParameter] GetTranslationIssuesRequest input)
    {
        var model = input.Model ?? "gpt-4";

        var prompt = "You are an expert linguist and your task is to perform a Language Quality Assessment on input sentences. " +
                     "Provide a quality rating for the original translation from 0 (completely bad) to 10 (perfect). " +
                     "Perform an LQA analysis and use the MQM 2.0 format. For each issue found, specify the category, description of the issue, and severity. " +
                     "Try to propose a correct translation that would have no LQA errors.";

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

        return new()
        {
            Message = chatResult.Choices.First().Message.Content
        };
    }

    [Action("Localize text", Description = "Localize the text provided")]
    public async Task<ChatResponse> LocalizeText([ActionParameter] LocalizeTextRequest input)
    {
        var model = input.Model ?? "gpt-4";
        var prompt = @$"
                    Original text: {input.Text}
                    Locale: {input.Locale}
                    Localized text:
                    ";
        var tikToken = await TikToken.GetEncodingAsync("cl100k_base");
        var maximumTokensNumber = tikToken.Encode(input.Text).Count + 100;

        var chatResult = await Client.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromUser(prompt),
            },
            MaxTokens = maximumTokensNumber,
            Model = model,
            Temperature = 0.1f
        });
        chatResult.ThrowOnError();

        return new()
        {
            Message = chatResult.Choices.First().Message.Content
        };
    }    

    #endregion
}