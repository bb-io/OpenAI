using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Linq;
using Blackbird.Applications.Sdk.Common.Actions;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using OpenAI.Interfaces;
using OpenAI.Extensions;
using Apps.OpenAI.Models.Responses;
using Apps.OpenAI.Models.Requests;
using System.Threading.Tasks;
using OpenAI.ObjectModels.ResponseModels;
using TiktokenSharp;

namespace Apps.OpenAI;

[ActionList]
public class Actions
{
    [Action("Generate completion", Description = "Completes the given prompt")]
    public async Task<CompletionResponse> CreateCompletion(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders, 
        [ActionParameter] CompletionRequest input)
    {
        var model = input.Model ?? "text-davinci-003";
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);
            
        var completionResult = await openAIService.Completions.CreateCompletion(new CompletionCreateRequest
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
        ThrowOnError(completionResult);

        return new CompletionResponse { CompletionText = completionResult.Choices.FirstOrDefault()?.Text };
    }

    [Action("Create summary", Description = "Summarizes the input text")]
    public async Task<SummaryResponse> CreateSummary(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] SummaryRequest input)
    {
        var model = input.Model ?? "text-davinci-003";
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var prompt = @$"
                Summarize the following text.

                Text:
                """"""
                {input.Text}
                """"""

                Summary:
            ";

        var completionResult = await openAIService.Completions.CreateCompletion(new CompletionCreateRequest
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
        ThrowOnError(completionResult);

        return new SummaryResponse { Summary = completionResult.Choices.FirstOrDefault()?.Text };
    }

    [Action("Generate edit", Description = "Edit the input text given an instruction prompt")]
    public async Task<EditResponse> CreateEdit(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] EditRequest input)
    {
        var model = input.Model ?? "text-davinci-edit-001";
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var editResult = await openAIService.Edit.CreateEdit(new EditCreateRequest
        {
            Input = input.InputText,
            Instruction = input.Instruction,
            Model = model,
            Temperature = input.Temperature,
            TopP = input.TopP,
        });
        ThrowOnError(editResult);

        return new EditResponse { EditText = editResult.Choices.FirstOrDefault()?.Text };
    }

    [Action("Chat", Description = "Gives a response given a chat message")]
    public async Task<ChatResponse> ChatMessageRequest(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] ChatRequest input)
    {
        var model = input.Model ?? "gpt-4";
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var chatResult = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
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
        ThrowOnError(chatResult);

        return new ChatResponse { Message = chatResult.Choices.FirstOrDefault()?.Message.Content };
    }

    [Action("Chat with system prompt", Description = "Gives a response given a chat message and a configurable system prompt")]
    public async Task<ChatResponse> ChatWithSystemMessageRequest(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] SystemChatRequest input)
    {
        var model = input.Model ?? "gpt-4";
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var chatResult = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
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
        ThrowOnError(chatResult);

        return new ChatResponse { Message = chatResult.Choices.FirstOrDefault()?.Message.Content };
    }

    [Action("Post-edit MT", Description = "Review MT translated text and generate a post-edited version")]
    public async Task<EditResponse> PostEditRequest(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] PostEditRequest input)
    {
        var model = input.Model ?? "gpt-4";
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var prompt = "You are receiving a source text that was translated by NMT into target text. Review the " +
                     "target text and respond with edits of the target text as necessary. If no edits required, " +
                     "respond with target text.";

        if (input.AdditionalPrompt != null)
            prompt = $"{prompt} {input.AdditionalPrompt}";

        var chatResult = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
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
            MaxTokens = input.TargetText.Count(),
            Model = model
        });

        ThrowOnError(chatResult);

        return new EditResponse { EditText = chatResult.Choices.FirstOrDefault()?.Message.Content };
    }
        
    [Action("Get translation issues", Description = "Review text translation and generate a comment with the issue description")]
    public async Task<ChatResponse> GetTranslationIssues(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] GetTranslationIssuesRequest input)
    {
        var model = input.Model ?? "gpt-4"; 
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var chatResult = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem("You are receiving a source text that was translated by NMT into target text. " +
                                       "Review the target text and respond with the issue description."),
                ChatMessage.FromUser(@$"
                        Source text: 
                        {input.SourceText}

                        Target text: 
                        {input.TargetText}
                    "),
            },
            MaxTokens = input.TargetText.Count(),
            Model = model
        });
        ThrowOnError(chatResult);

        return new() { Message = chatResult.Choices.FirstOrDefault()?.Message.Content };
    }

    [Action("Generate image", Description = "Generates an image based on a prompt")]
    public async Task<ImageResponse> GenerateImage(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] ImageRequest input)
    {
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);
            
        var imageResult = await openAIService.Image.CreateImage(new ImageCreateRequest
        {
            Prompt = input.Prompt,
            ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url,
            N = 1,
            Size = input.Size,
        });
        ThrowOnError(imageResult);

        return new ImageResponse { Url = imageResult.Results.FirstOrDefault()?.Url };
    }

    [Action("Create transcription", Description = "Generates a transcription given an audio or video file. ( mp3, " +
                                                  "mp4, mpeg, mpga, m4a, wav, or webm)")]
    public async Task<TranscriptionResponse> CreateTranscription(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] TranscriptionRequest input)
    {
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var audioResult = await openAIService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest
        {
            FileName = input.FileName ?? input.File.Name,
            File = input.File.Bytes,
            Model = "whisper-1",
            ResponseFormat = StaticValues.AudioStatics.ResponseFormat.VerboseJson,
            Language = input.Language,
            Temperature = input.Temperature,
        });
        ThrowOnError(audioResult);

        return new TranscriptionResponse { Transcription = audioResult.Text };
    }

    [Action("Create English translation", Description = "Generates a translation into English given an audio or " +
                                                        "video file (mp3, mp4, mpeg, mpga, m4a, wav, or webm).")]
    public async Task<TranslationResponse> CreateTranslation(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] TranslationRequest input)
    {
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var audioResult = await openAIService.Audio.CreateTranslation(new AudioCreateTranscriptionRequest
        {
            FileName = input.FileName ?? input.File.Name,
            File = input.File.Bytes,
            Model = "whisper-1",
            ResponseFormat = StaticValues.AudioStatics.ResponseFormat.VerboseJson,
            Temperature = input.Temperature
        });
        ThrowOnError(audioResult);

        return new TranslationResponse { TranslatedText = audioResult.Text };
    }

    [Action("Create embedding", Description = "Generate an embedding for a text provided. An embedding is a list of " +
                                              "floating point numbers that captures semantic information about the " +
                                              "text that it represents.")]
    public async Task<EmbeddingResponse> CreateEmbedding(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] EmbeddingRequest input)
    {
        var model = input.Model ?? "text-embedding-ada-002";
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var embedResult = await openAIService.Embeddings.CreateEmbedding(new EmbeddingCreateRequest
        {
            Input = input.Text,
            Model = model
        });
        ThrowOnError(embedResult);

        return new() { Embedding = embedResult.Data.First().Embedding };
    }
        
    [Action("Localize text", Description = "Localize the text provided")]
    public async Task<ChatResponse> LocalizeText(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        [ActionParameter] LocalizeTextRequest input)
    {
        var model = input.Model ?? "gpt-4";
        var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

        var prompt = @$"
                    Original text: {input.Text}
                    Locale: {input.Locale}
                    Localized text:
                    ";
        var tikToken = await TikToken.GetEncodingAsync("cl100k_base");
        var maximumTokensNumber = tikToken.Encode(input.Text).Count + 100;
            
        var chatResult = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromUser(prompt),
            },
            MaxTokens = maximumTokensNumber,
            Model = model,
            Temperature = 0.1f
        });
        ThrowOnError(chatResult);

        return new ChatResponse { Message = chatResult.Choices.FirstOrDefault()?.Message.Content };
    }

    [Action("Tokenize text", Description = "Tokenize the text provided. Optionally specify encoding: cl100k_base " +
                                           "(used by gpt-4, gpt-3.5-turbo, text-embedding-ada-002) or p50k_base " +
                                           "(used by codex models, text-davinci-002, text-davinci-003).")]
    public async Task<TokenizeTextResponse> TokenizeText([ActionParameter] TokenizeTextRequest input)
    {
        var encoding = input.Encoding ?? "cl100k_base";
        var tikToken = await TikToken.GetEncodingAsync(encoding);
        var tokens = tikToken.Encode(input.Text);
        return new TokenizeTextResponse { Tokens = tokens };
    }

    private void ThrowOnError(BaseResponse response)
    {
        if (!response.Successful)
        {
            if (response.Error == null)
                throw new Exception("Unknown error");

            throw new Exception($"{response.Error.Code}: {response.Error.Message}");
        }
    }

    private IOpenAIService CreateOpenAIServiceSdk(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders)
    {
        var organization = authenticationCredentialsProviders.First(p => p.KeyName == "Organization ID").Value;
        var apiKey = authenticationCredentialsProviders.First(p => p.KeyName == "API key").Value;

        var connectionParams = new Dictionary<string, string>(){
            {"Organization",  organization},
            {"ApiKey", apiKey}
        };
        var apiSettings = new Dictionary<string, Dictionary<string, string>> {
            {"OpenAIServiceOptions", connectionParams}
        };
        var apiSettingsJson = JsonConvert.SerializeObject(apiSettings);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(apiSettingsJson))).Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => configuration);
        serviceCollection.AddOpenAIService();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var sdk = serviceProvider.GetRequiredService<IOpenAIService>();
        return sdk;
    }
}