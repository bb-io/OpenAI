using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Requests.Image;
using Apps.OpenAI.Models.Responses.Image;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.OpenAI.Actions;

[ActionList]
public class ImageActions : BaseActions
{
    public ImageActions(InvocationContext invocationContext) : base(invocationContext) { }

    [Action("Generate image", Description = "Generates an image based on a prompt")]
    public async Task<ImageResponse> GenerateImage([ActionParameter] ImageRequest input)
    {
        var request = new OpenAIRequest("/images/generations", Method.Post, Creds);
        request.AddJsonBody(new
        {
            prompt = input.Prompt,
            response_format = "url",
            size = input.Size ?? "1024x1024"
        });

        var response = await Client.ExecuteWithErrorHandling<DataDto<ImageDataDto>>(request);
        return new()
        {
            Url = response.Data.FirstOrDefault()?.Url ?? throw new("Unable to create image")
        };
    }
}