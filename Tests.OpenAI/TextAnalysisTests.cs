using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Analysis;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class TextAnalysisTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task CreateEmbedding_AzureOpenAi_ThrowsApplicationException(InvocationContext context)
    {
        // Arrange
        var action = new TextAnalysisActions(context, FileManagementClient);
        var request = new EmbeddingRequest { Text = "This needs to be embedded" };
        var identifier = new EmbeddingModelIdentifier { ModelId = "" };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginApplicationException>(async () => 
            await action.CreateEmbedding(identifier, request)
        );

        // Assert
        Assert.Contains("does not work with the specified model", ex.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task CreateEmbedding_OpenAi_ReturnsCreateEmbeddingResponse(InvocationContext context)
    {
        // Arrange
        var action = new TextAnalysisActions(context, FileManagementClient);
        var request = new EmbeddingRequest { Text = "This needs to be embedded" };
        var identifier = new EmbeddingModelIdentifier { ModelId = null };

        // Act
        var result = await action.CreateEmbedding(identifier, request);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }
}
