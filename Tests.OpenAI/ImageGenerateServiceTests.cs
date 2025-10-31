using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Image;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ImageGenerateServiceTests : TestBase
{
    [TestMethod]
    public async Task GenerateImage_OpenAiEmbedded_ReturnsGeneratedImage()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAiEmbedded);
        var handler = new ImageActions(context, FileManagementClient);
        var modelId = new ImageGenerationModelIdentifier { ModelId = "dall-e-2" };
        var prompt = new ImageRequest { Prompt = "Generate photo of cat with donuts" };

        // Act
        var data = await handler.GenerateImage(modelId, prompt);

        // Assert
        Assert.IsNotNull(data);
    }

    [TestMethod]
    public async Task GenerateImage_AzureOpenAi_ThrowsMisconfigException()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAiEmbedded);
        var handler = new ImageActions(context, FileManagementClient);
        var modelId = new ImageGenerationModelIdentifier { ModelId = "gpt-image-1" };
        var prompt = new ImageRequest { Prompt = "Generate photo of cat with donuts" };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () => 
            await handler.GenerateImage(modelId, prompt)
        );

        // Assert
        StringAssert.Contains(ex.Message, "Azure OpenAI does not support image actions");
    }
}
