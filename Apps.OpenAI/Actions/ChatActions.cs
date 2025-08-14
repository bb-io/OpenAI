using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Chat;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;

namespace Apps.OpenAI.Actions;

[ActionList("Chat")]
public class ChatActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    private const int MaxCompletionRetries = 3;
    
    [Action("Chat", Description = "Gives a response given a chat message")]
    public async Task<ChatResponse> ChatMessageRequest([ActionParameter] TextChatModelIdentifier modelIdentifier,
        [ActionParameter] ChatRequest input,
        [ActionParameter] GlossaryRequest glossary)
    {
        if (input.File != null)
        {
            if (input.File.ContentType.StartsWith("audio") || input.File.Name.EndsWith("wav") || input.File.Name.EndsWith("mp3"))
            {
                modelIdentifier.ModelId = "gpt-4o-audio-preview";
            }
            if (input.File.ContentType.StartsWith("image") || input.File.Name.EndsWith("png") || input.File.Name.EndsWith("jpg") || input.File.Name.EndsWith("jpeg")|| input.File.Name.EndsWith("webp") || input.File.Name.EndsWith("gif"))
            {
                modelIdentifier.ModelId = "gpt-4-vision-preview";
            }
        }
        
        var messages = await GenerateChatMessages(input, glossary);
        var completeMessage = string.Empty;
        var usage = new UsageDto();
        var counter = 0;
        
        while (counter < MaxCompletionRetries)
        {
            var response = await ExecuteChatCompletion(messages, modelIdentifier.GetModel(), input);
            completeMessage += response.Choices.First().Message.Content;

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
            File = input.Image,
            Parameters = input.Parameters,
            PresencePenalty = input.PresencePenalty,
            Temperature = input.Temperature,
            TopP = input.TopP
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
            if (input.SystemPrompt != null)
            {
                messages.Add(new ChatMessageDto(MessageRoles.System, input.SystemPrompt));
            }
            
            if (input.File.ContentType.StartsWith("audio") || input.File.Name.EndsWith("wav") || input.File.Name.EndsWith("mp3"))
            {
                messages.Add(new ChatAudioMessageDto(MessageRoles.User, new List<ChatAudioMessageContentDto>
                {

                    new ChatAudioMessageTextContentDto("text", input.Message),
                    new ChatAudioMessageAudioContentDto("input_audio", new InputAudio(){Format = input.File.Name.Substring(input.File.Name.Length-3).ToLower(),Data = Convert.ToBase64String(fileBytes) })
                }));
            }
            if (input.File.ContentType.StartsWith("image") || input.File.Name.EndsWith("png") || input.File.Name.EndsWith("jpg") || input.File.Name.EndsWith("jpeg") || input.File.Name.EndsWith("webp") || input.File.Name.EndsWith("gif"))
            {
                messages.Add(new ChatImageMessageDto(MessageRoles.User, new List<ChatImageMessageContentDto>
                {
                    new ChatImageMessageTextContentDto("text", input.Message),
                    new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                        $"data:{input.File.ContentType};base64,{Convert.ToBase64String(fileBytes)}"))
                }));
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

}