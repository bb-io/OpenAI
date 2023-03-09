using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.GPT3.Extensions;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Apps.OpenAI.Model.Requests;
using Apps.OpenAI.Model.Responses;
using System.Linq;


namespace Apps.OpenAI
{
    [ActionList]
    public class Actions
    {
        [Action]
        public CompletionResponse CreateCompletion(string organizationId, AuthenticationCredentialsProvider authenticationCredentialsProvider, 
            [ActionParameter] CompletionRequest input)
        {
            var openAIService = GetOpenAIServiceSdk(organizationId, authenticationCredentialsProvider.Value);

            var completionResult = openAIService.Completions.CreateCompletion(new CompletionCreateRequest
            {
                Prompt = input.Prompt,
                MaxTokens = input.MaximumTokens,
                LogProbs = 1
            }, Models.Davinci).Result.Choices.FirstOrDefault().Text;

            return new CompletionResponse(){ CompletionText = completionResult };
        }

        [Action]
        public EditResponse CreateEdit(string organizationId, AuthenticationCredentialsProvider authenticationCredentialsProvider,
            [ActionParameter] EditRequest input)
        {
            var openAIService = GetOpenAIServiceSdk(organizationId, authenticationCredentialsProvider.Value);

            var editResult = openAIService.Edit.CreateEdit(new EditCreateRequest
            {
                Input = input.InputText,
                Instruction = input.Instruction
            }, Models.TextEditDavinciV1).Result.Choices.FirstOrDefault().Text;

            return new EditResponse() { EditText = editResult };
        }

        [Action]
        public ChatResponse ChatMessageRequest(string organizationId, AuthenticationCredentialsProvider authenticationCredentialsProvider,
            [ActionParameter] ChatRequest input)
        {
            var openAIService = GetOpenAIServiceSdk(organizationId, authenticationCredentialsProvider.Value);

            var chatResult = openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromUser(input.Message),
                },
                MaxTokens = input.MaximumTokens,
                Model = Models.ChatGpt3_5Turbo
            });

            return new ChatResponse() { Message = chatResult.Result.Choices.FirstOrDefault().Message.Content };
        }

        private IOpenAIService GetOpenAIServiceSdk(string organization, string apiKey)
        {
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
}
