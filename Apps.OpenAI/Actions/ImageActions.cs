using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Extensions;
using Apps.OpenAI.Invocables;
using Apps.OpenAI.Models.Requests.Image;
using Apps.OpenAI.Models.Responses.Image;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace Apps.OpenAI.Actions;

[ActionList]
public class ImageActions : OpenAiInvocable
{
    private IOpenAIService Client { get; }

    public ImageActions(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = Creds.CreateOpenAiServiceSdk();
    }

    [Action("Generate image", Description = "Generates an image based on a prompt")]
    public async Task<ImageResponse> GenerateImage([ActionParameter] ImageRequest input)
    {
        var imageResult = await Client.Image.CreateImage(new ImageCreateRequest
        {
            Prompt = input.Prompt,
            ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url,
            N = 1,
            Size = input.Size,
        });
        imageResult.ThrowOnError();

        return new()
        {
            Url = imageResult.Results.FirstOrDefault()?.Url ?? throw new("Unable to create image")
        };
    }
}