using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Image;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ImageGenerateServiceTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task GenerateImage_OpenAiEmbedded_ReturnsGeneratedImage(InvocationContext context)
    {
        // Arrange
        var handler = new ImageActions(context, FileManagementClient);
        var modelId = new ImageGenerationModelIdentifier { ModelId = "dall-e-2" };
        var prompt = new ImageRequest { Prompt = "Generate photo of cat with donuts" };

        // Act
        var data = await handler.GenerateImage(modelId, prompt);

        // Assert
        Assert.IsNotNull(data);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task GenerateImage_AzureOpenAi_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var handler = new ImageActions(context, FileManagementClient);
        var modelId = new ImageGenerationModelIdentifier { ModelId = "gpt-image-1" };
        var prompt = new ImageRequest { Prompt = "Generate photo of cat with donuts" };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () => 
            await handler.GenerateImage(modelId, prompt)
        );

        // Assert
        Assert.Contains("Azure OpenAI does not support image actions", ex.Message);
    }
}
