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
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Filters.Extensions;

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

        var files = input.File?.ToList();
        if (files != null && files.Count != 0)
        {
            if (files.Any(x => x.IsAudio()))
            {
                // The Completions API did support audio inputs, but the Responses API doesn't
                throw new PluginMisconfigurationException(
                    "OpenAI does not support audio files for chat endpoints. " +
                    "Please use Audio actions for such files");
            }

            if (files.Count > 1)
            {
                if (files.All(x => x.IsImage()))
                {
                    messages.Add(await BuildImageMessageAsync(files, input.Message));
                }
                else if (files.All(x => x.IsSupportedFileType() && !x.IsImage()))
                {
                    messages.Add(await BuildSupportedFilesMessageAsync(files, input.Message));
                }
                else if (files.Any(x => x.IsImage()))
                {
                    throw new PluginMisconfigurationException(
                        "Mixed image and document file sets are not supported yet. " +
                        "Please provide either only images or only supported document files.");
                }
                else
                {
                    throw new PluginMisconfigurationException(
                        "Multiple files are supported only for images and OpenAI-supported document file types.");
                }
            }
            else
            {
                var file = files[0];

                if (file.IsImage())
                {
                    messages.Add(await BuildImageMessageAsync(files, input.Message));
                }
                else if (file.IsSupportedFileType())
                {
                    messages.Add(await BuildSupportedFilesMessageAsync(files, input.Message));
                }
                else
                {
                    var fileStream = await FileManagementClient.DownloadAsync(file);
                    var fileBytes = await fileStream.GetByteData();
                    var content = Encoding.UTF8.GetString(fileBytes);
                    using var contentStream = content.ToStream();
                    var loadResult = Transformation.Load(contentStream, file.Name, file.ContentType);
                    var text = content;

                    if (loadResult.Success)
                    {
                        var targetContentResult = loadResult.Value.Target();
                        if (!targetContentResult.Success)
                            throw new PluginMisconfigurationException(targetContentResult.Error);
                        var targetContent = targetContentResult.Value;
                        text = targetContent.GetPlaintext();
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            var sourceContentResult = loadResult.Value.Source();
                            if (!sourceContentResult.Success)
                                throw new PluginMisconfigurationException(sourceContentResult.Error);
                            var sourceContent = sourceContentResult.Value;
                            text = sourceContent.GetPlaintext();
                        }
                    }

                    messages.Add(new ChatMessageDto(MessageRoles.User, input.Message));
                    messages.Add(new ChatMessageDto(MessageRoles.User, $"File content:\r\n{text}"));
                }
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

    private async Task<ChatImageMessageDto> BuildImageMessageAsync(IEnumerable<FileReference> files, string message)
    {
        var content = new List<ChatImageMessageContentDto>
        {
            new ChatImageMessageTextContentDto("text", message)
        };

        foreach (var file in files)
        {
            var fileStream = await FileManagementClient.DownloadAsync(file);
            var fileBytes = await fileStream.GetByteData();
            var contentType = GetRequiredContentType(file);

            content.Add(new ChatImageMessageImageContentDto(
                "image_url",
                new ImageUrlDto(FileHelper.GenerateBase64String(contentType, fileBytes))));
        }

        return new ChatImageMessageDto(MessageRoles.User, content);
    }

    private async Task<ChatFileMessageDto> BuildSupportedFilesMessageAsync(IEnumerable<FileReference> files, string message)
    {
        var contentParts = new List<object>();

        foreach (var file in files)
        {
            var fileStream = await FileManagementClient.DownloadAsync(file);
            var fileBytes = await fileStream.GetByteData();
            var contentType = GetRequiredContentType(file);

            contentParts.Add(new ChatInputFileContentDto(
                "input_file",
                file.Name,
                FileHelper.GenerateBase64String(contentType, fileBytes)));
        }

        contentParts.Add(new ChatInputTextContentDto("input_text", message));

        return new ChatFileMessageDto(MessageRoles.User, contentParts);
    }

    private static string GetRequiredContentType(FileReference file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType;
        }

        throw new PluginMisconfigurationException(
            $"Content type is required for chat file input '{file.Name}'.");
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
