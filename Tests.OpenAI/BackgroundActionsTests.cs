using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Requests.Background;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class BackgroundActionsTests : TestBase
{
    [TestMethod]
    public async Task DownloadContentFromBackground_OpenAiCompletedBatchWithXliffFile_Success()
    {
        var context = GetInvocationContext(ConnectionTypes.OpenAi);
        var actions = new BackgroundActions(context, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_68e4a64badd8819082197f5fef3306b5",
            TransformationFile = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" }
        };
            
        var result = await actions.DownloadContentFromBackground(downloadRequest);

        PrintResult(context, result);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task DownloadContentFromBackground_AzureOpenAiCompletedBatchWithXliffFile_Success()
    {
        var context = GetInvocationContext(ConnectionTypes.AzureOpenAi);
        var actions = new BackgroundActions(context, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_0031dcb6-84cb-4d15-b58f-e846b0f44dab",
            TransformationFile = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" }
        };

        var result = await actions.DownloadContentFromBackground(downloadRequest);

        PrintResult(context, result);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetMqmReportFromBackground_OpenAiEmbeddedCompletedBatch_Success()
    {
        var context = GetInvocationContext(ConnectionTypes.OpenAiEmbedded);
        var actions = new BackgroundActions(context, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_68e4168ac48c81909609edd7ea536873",
            TransformationFile = new FileReference { Name = "mqm.xlf" }
        };
            
        var result = await actions.GetMqmReportFromBackground(downloadRequest);
            
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task GetMqmReportFromBackground_AzureOpenAiCompletedBatch_Success()
    {
        var context = GetInvocationContext(ConnectionTypes.AzureOpenAi);
        var actions = new BackgroundActions(context, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_0031dcb6-84cb-4d15-b58f-e846b0f44dab",
            TransformationFile = new FileReference { Name = "mqm.xlf" }
        };

        var result = await actions.GetMqmReportFromBackground(downloadRequest);

        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}