using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Requests.Background;
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
            BatchId = "batch_68d52abe5dc481908c1c78739908e7ba",
            TransformationFile = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" }
        };
        
        var result = await actions.DownloadContentFromBackground(downloadRequest);
        
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
    
    [TestMethod]
    public async Task GetMqmReportFromBackground_CompletedBatch_Success()
    {
        var actions = new BackgroundActions(InvocationContext, FileManagementClient);
        var downloadRequest = new BackgroundDownloadRequest
        {
            BatchId = "batch_68d524a2d59c8190a89421d2c37f195a",
            TransformationFile = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "mqm.xlf" }
        };
        
        var result = await actions.GetMqmReportFromBackground(downloadRequest);
        
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}