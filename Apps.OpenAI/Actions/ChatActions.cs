using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Utils;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Content;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Actions;

[ActionList("Chat")]
public class ChatActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    private const int MaxCompletionRetries = 3;

    [Action("Chat", Description = "Outputs a response to a chat message.")]
    public async Task<ChatResponse> ChatMessageRequest(
        [ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ChatRequest input,
        [ActionParameter] GlossaryRequest glossary)
    {
        HandleInput(input);

        var messages = await GenerateChatMessages(input, glossary);
        var completeMessage = string.Empty;
        var usage = new UsageDto();
        var citations = new List<UrlCitationDto>();
        var sources = new HashSet<string>();
        var counter = 0;
        
        while (counter < MaxCompletionRetries)
        {
            var response = await ExecuteApiRequestAsync(messages, modelIdentifier.ModelId, input);
            completeMessage += response.Choices.First().Message.Content;

            citations.AddRange(response.Citations);
            foreach (var source in response.Sources)
            {
                sources.Add(source);
            }

            usage += response.Usage;

            if (response.Choices.First().FinishReason != "length")
            {
                break;
            }

            messages.Add(new ChatMessageDto(MessageRoles.Assistant, response.Choices.First().Message.Content));
            messages.Add(new ChatMessageDto(MessageRoles.User, "Continue your latest message, it was too long."));
            counter += 1;
        }

        return new()
        {
            Message = completeMessage,
            SystemPrompt = messages.Where(x => x.GetType() == typeof(ChatMessageDto) && x.Role == MessageRoles.System)
                .Select(x => ((ChatMessageDto)x).Content).FirstOrDefault() ?? string.Empty,
            UserPrompt = messages.Where(x => x.GetType() == typeof(ChatMessageDto) && x.Role == MessageRoles.User)
                .Select(x => ((ChatMessageDto)x).Content).FirstOrDefault() ?? string.Empty,
            Usage = usage,
            Citations = citations
                .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                .GroupBy(x => x.Url)
                .Select(x => x.First())
                .ToList(),
            Sources = sources.ToList()
        };
    }

    // This action may seem redundant but we feel that the optional system prompt in the action above may be too hidden.
    [Action("Chat with system prompt", Description = "Outputs a response to a chat message with a required system prompt.")]
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
            File = input.File,
            Parameters = input.Parameters,
            PresencePenalty = input.PresencePenalty,
            Temperature = input.Temperature,
            TopP = input.TopP,
            ReasoningEffort = input.ReasoningEffort,
            EnableWebSearch = input.EnableWebSearch,
            WebSearchContextSize = input.WebSearchContextSize,
            ExternalWebAccess = input.ExternalWebAccess,
            AllowedDomains = input.AllowedDomains,
            UserLocationCity = input.UserLocationCity,
            UserLocationCountry = input.UserLocationCountry,
            UserLocationRegion = input.UserLocationRegion,
            UserLocationTimezone = input.UserLocationTimezone
        }, glossary);
    }

    private async Task<List<BaseChatMessageDto>> GenerateChatMessages(ChatRequest input, GlossaryRequest? request)
    {
        var messages = new List<BaseChatMessageDto>();
        if (input.SystemPrompt != null)
        {
            messages.Add(new ChatMessageDto(MessageRoles.System, input.SystemPrompt));
        }

        if (input.File != null)
        {
            var fileStream = await FileManagementClient.DownloadAsync(input.File);
            var fileBytes = await fileStream.GetByteData();
            
            if (input.File.IsAudio())
            {
                // The Completions API did support audio inputs, but the Responses API doesn't
                throw new PluginMisconfigurationException(
                    "OpenAI does not support audio files for chat endpoints. " +
                    "Please use Audio actions for such files");
            }
            
            if (input.File.IsImage())
            {
                messages.Add(new ChatImageMessageDto(MessageRoles.User, new List<ChatImageMessageContentDto>
                {
                    new ChatImageMessageTextContentDto("text", input.Message),
                    new ChatImageMessageImageContentDto(
                        "image_url", 
                        new ImageUrlDto($"data:{input.File.ContentType};base64,{Convert.ToBase64String(fileBytes)}"))
                }));
            }
            else
            {
                var content = Encoding.UTF8.GetString(fileBytes);

                CodedContent codedContent = null;
                if (Xliff2Serializer.IsXliff2(content))
                {
                    var transformation = Transformation.Parse(content, input.File.Name);
                    codedContent = transformation.Target();
                }
                else
                {
                    TryCatchHelper.TryCatch(
                        () => codedContent = CodedContent.Parse(content, input.File.Name), 
                        $"Can't process an input file with type {input.File.ContentType}"
                    );
                }

                var text = codedContent.GetPlaintext();
                messages.Add(new ChatMessageDto(MessageRoles.User, input.Message));
                messages.Add(new ChatMessageDto(MessageRoles.User, $"File content:\r\n{text}"));
            }
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

                var prompt = $"{input.Message}; Parameters that you should use (they can be in json format): {stringBuilder}";
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
            {
                messages.Add(new ChatMessageDto(MessageRoles.User, $"Glossary: {glossaryPromptPart}"));
            }
        }

        return messages;
    }

    private void HandleInput(ChatRequest input)
    {
        if (input.EnableWebSearch == true && UniversalClient.ConnectionType == ConnectionTypes.AzureOpenAi)
        {
            throw new PluginMisconfigurationException(
                "Web search is not supported for Azure OpenAI connections in this action. Please use an OpenAI connection.");
        }
    }
}
