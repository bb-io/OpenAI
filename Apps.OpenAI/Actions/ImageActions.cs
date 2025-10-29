using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Image;
using Apps.OpenAI.Models.Responses.Chat;
using Apps.OpenAI.Models.Responses.Image;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.OpenAI.Actions;

[ActionList("Images")]
public class ImageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Generate image", Description = "Generates an image based on a prompt")]
    public async Task<ImageResponse> GenerateImage([ActionParameter] ImageGenerationModelIdentifier modelIdentifier,
        [ActionParameter] ImageRequest input)
    {
        ThrowForAzure("image");

        var model = modelIdentifier.ModelId ?? "dall-e-3";
        var request = new OpenAIRequest("/images/generations", Method.Post);

        if (model == "dall-e-3")
        {
            request.AddJsonBody(new
            {
                model,
                prompt = input.Prompt,
                response_format = "b64_json",
                size = input.Size ?? "1024x1024",
                quality = input.Quality ?? "standard",
                style = input.Style ?? "vivid"
            });
        }
        else if (model == "gpt-image-1")
        {
            request.AddJsonBody(new
            {
                model,
                prompt = input.Prompt,
                size = input.Size ?? "1024x1024"
            });
        }
        else
        {
            request.AddJsonBody(new
            {
                model,
                prompt = input.Prompt,
                response_format = "b64_json",
                size = input.Size ?? "1024x1024"
            });
        }

        var response = await UniversalClient.ExecuteWithErrorHandling<DataDto<ImageDataDto>>(request);
        var bytes = Convert.FromBase64String(response.Data.First().Base64);

        using var stream = new MemoryStream(bytes);
        var filename = (input.OutputImageName ?? "image") + ".png";
        var file = await FileManagementClient.UploadAsync(stream, "image/png", filename);
        return new() { Image = file };
    }

    [Action("Get localizable content from image", Description = "Retrieve localizable content from image.")]
    public async Task<ChatResponse> GetLocalizableContentFromImage(
        [ActionParameter] ImageChatModelIdentifier modelIdentifier,
        [ActionParameter] GetLocalizableContentFromImageRequest input)
    {
        ThrowForAzure("image");

        var prompt = "Your objective is to conduct optical character recognition (OCR) to identify and extract any " +
                     "localizable content present in the image. Respond with the text found in the image, if any. " +
                     "If no localizable content is detected, provide an empty response.";

        var fileStream = await FileManagementClient.DownloadAsync(input.Image);
        var fileBytes = await fileStream.GetByteData();
        var messages = new List<ChatImageMessageDto>
            {
                new(MessageRoles.User, new List<ChatImageMessageContentDto>
                {
                    new ChatImageMessageTextContentDto("text", prompt),
                    new ChatImageMessageImageContentDto("image_url", new ImageUrlDto(
                        $"data:{input.Image.ContentType};base64,{Convert.ToBase64String(fileBytes)}"))
                })
            };
        var response = await ExecuteChatCompletion(messages, modelIdentifier.ModelId, input);

        return new()
        {
            SystemPrompt = prompt,
            UserPrompt = "",
            Message = response.Choices.First().Message.Content,
            Usage = response.Usage,
        };
    }
}