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
            BatchId = "batch_68d51188ee588190ba8587d150668bc0",
            TransformationFile = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.xlf" }
        };
        
        var result = await actions.DownloadContentFromBackground(downloadRequest);
        
        Assert.IsNotNull(result);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}