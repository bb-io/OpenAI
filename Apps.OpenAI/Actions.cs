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
using Blackbird.Applications.Sdk.Common.Actions;

namespace Apps.OpenAI
{
    [ActionList]
    public class Actions
    {
        [Action("Complete", Description = "Completes the given prompt")]
        public CompletionResponse CreateCompletion(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders, 
            [ActionParameter] CompletionRequest input)
        {
            var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

            var completionResult = openAIService.Completions.CreateCompletion(new CompletionCreateRequest
            {
                Prompt = input.Prompt,
                MaxTokens = input.MaximumTokens,
                LogProbs = 1
            }, Models.Davinci).Result.Choices.FirstOrDefault().Text;

            return new CompletionResponse(){ CompletionText = completionResult };
        }

        [Action("Edit", Description = "Edit the input text given an instruction prompt")]
        public EditResponse CreateEdit(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] EditRequest input)
        {
            var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

            var editResult = openAIService.Edit.CreateEdit(new EditCreateRequest
            {
                Input = input.InputText,
                Instruction = input.Instruction
            }, Models.TextEditDavinciV1).Result.Choices.FirstOrDefault().Text;

            return new EditResponse() { EditText = editResult };
        }

        [Action("Chat", Description = "Gives a response given a chat message")]
        public ChatResponse ChatMessageRequest(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
            [ActionParameter] ChatRequest input)
        {
            var openAIService = CreateOpenAIServiceSdk(authenticationCredentialsProviders);

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

        private IOpenAIService CreateOpenAIServiceSdk(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders)
        {
            var organization = authenticationCredentialsProviders.First(p => p.KeyName == "organizationId").Value;
            var apiKey = authenticationCredentialsProviders.First(p => p.KeyName == "apiKey").Value;

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
