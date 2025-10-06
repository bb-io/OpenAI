using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Requests.Background;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Newtonsoft.Json;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class BackgroundActionsTests : TestBase
{
    [TestMethod]
    public async Task DownloadContentFromBackground_CompletedBatchWithXliffFile_Success()
    {
        var actions = new BackgroundActions(InvocationContext, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_68e4138f9e848190a06d371b38afa6fa",
            TransformationFile = new FileReference { Name = "test.xlf" }
        };
        
        var result = await actions.DownloadContentFromBackground(downloadRequest);
        
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task DownloadContentFromBackground_TooLongUri_Failure()
    {
        // Arrange
        var actions = new BackgroundActions(InvocationContext, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = new string('a', 50000),
            TransformationFile = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" },
        };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginApplicationException>(async () =>
            await actions.DownloadContentFromBackground(downloadRequest)
        );

        // Assert
        StringAssert.Contains(ex.Message, "URI Too Large");
    }

    [TestMethod]
    public async Task GetMqmReportFromBackground_CompletedBatch_Success()
    {
        var actions = new BackgroundActions(InvocationContext, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_68e4168ac48c81909609edd7ea536873",
            TransformationFile = new FileReference { Name = "mqm.xlf" }
        };
        
        var result = await actions.GetMqmReportFromBackground(downloadRequest);
        
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}