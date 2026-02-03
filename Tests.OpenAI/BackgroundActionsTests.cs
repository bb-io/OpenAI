using Tests.OpenAI.Base;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Requests.Background;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.OpenAI;

[TestClass]
public class BackgroundActionsTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task DownloadContentFromBackground_OpenAiCompletedBatchWithXliffFile_Success(InvocationContext context)
    {
        // Arrange
        var actions = new BackgroundActions(context, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_68e4a64badd8819082197f5fef3306b5",
            TransformationFile = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" }
        };

        // Act
        var result = await actions.DownloadContentFromBackground(downloadRequest);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task DownloadContentFromBackground_AzureOpenAiCompletedBatchWithXliffFile_Success(InvocationContext context)
    {
        // Arrange
        var actions = new BackgroundActions(context, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_0031dcb6-84cb-4d15-b58f-e846b0f44dab",
            TransformationFile = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" }
        };

        // Act
        var result = await actions.DownloadContentFromBackground(downloadRequest);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task GetMqmReportFromBackground_OpenAiCompletedBatch_Success(InvocationContext context)
    {
        // Arrange
        var actions = new BackgroundActions(context, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_6981d173a1248190ae03665c6fa26b74",
            TransformationFile = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" }
        };

        // Act
        var result = await actions.GetMqmReportFromBackground(downloadRequest);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task GetMqmReportFromBackground_AzureOpenAiCompletedBatch_Success(InvocationContext context)
    {
        // Arrange
        var actions = new BackgroundActions(context, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_0031dcb6-84cb-4d15-b58f-e846b0f44dab",
            TransformationFile = new FileReference { Name = "mqm.xlf" }
        };

        // Act
        var result = await actions.GetMqmReportFromBackground(downloadRequest);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }
}