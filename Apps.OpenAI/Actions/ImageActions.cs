using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Image;
using Apps.OpenAI.Models.Responses.Image;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.OpenAI.Actions;

[ActionList("Images")]
public class ImageActions : BaseActions
{
    public ImageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
        : base(invocationContext, fileManagementClient)
    {
    }

    [Action("Generate image", Description = "Generates an image based on a prompt")]
    public async Task<ImageResponse> GenerateImage([ActionParameter] ImageGenerationModelIdentifier modelIdentifier,
        [ActionParameter] ImageRequest input)
    {
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

        var response = await Client.ExecuteWithErrorHandling<DataDto<ImageDataDto>>(request);
        var bytes = Convert.FromBase64String(response.Data.First().Base64);

        using var stream = new MemoryStream(bytes);
        var filename = (input.OutputImageName ?? "image") + ".png";
        var file = await FileManagementClient.UploadAsync(stream, "image/png", filename);
        return new() { Image = file };
    }
}