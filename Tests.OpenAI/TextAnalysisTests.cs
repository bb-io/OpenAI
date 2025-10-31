using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Analysis;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class TextAnalysisTests : TestBase
{
    [TestMethod]
    public async Task CreateEmbedding_AzureOpenAi_ReturnsCreateEmbeddingResponse()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.AzureOpenAi);
        var action = new TextAnalysisActions(context, FileManagementClient);
        var request = new EmbeddingRequest { Text = "This needs to be embedded" };
        var identifier = new EmbeddingModelIdentifier { ModelId = "" };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginApplicationException>(async () => 
            await action.CreateEmbedding(identifier, request)
        );

        // Assert
        StringAssert.Contains(ex.Message, "does not work with the specified model");
    }

    [TestMethod]
    public async Task CreateEmbedding_OpenAi_ReturnsCreateEmbeddingResponse()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAi);
        var action = new TextAnalysisActions(context, FileManagementClient);
        var request = new EmbeddingRequest { Text = "This needs to be embedded" };
        var identifier = new EmbeddingModelIdentifier { ModelId = null };

        // Act
        var result = await action.CreateEmbedding(identifier, request);

        // Assert
        PrintResult(context, result);
        Assert.IsNotNull(result);
    }
}
