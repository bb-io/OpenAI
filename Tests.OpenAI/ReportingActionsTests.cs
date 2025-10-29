using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Requests.Background;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ReportingActionsTests : TestBase
{
    [TestMethod]
    public async Task CreateMqmReportInBackground_OpenAiEmbeddedXliffFile_Success()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAiEmbedded);
        var actions = new ReportingActions(context, FileManagementClient);
        var request = new CreateMqmReportInBackgroundRequest()
        {
            ModelId = "gpt-4.1",
            File = new FileReference { Name = "mqm.xlf" },
            TargetLanguage = "fr"
        };

        // Act
        var result = await actions.CreateMqmReportInBackground(request);

        // Assert           
        Assert.IsNotNull(result);
        PrintResult(context, result);
    }

    [TestMethod]
    public async Task CreateMqmReportInBackground_AzureOpenAiXliffFile_Success()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.AzureOpenAi);
        var actions = new ReportingActions(context, FileManagementClient);
        var request = new CreateMqmReportInBackgroundRequest()
        {
            ModelId = "gpt-4.1",
            File = new FileReference { Name = "mqm.xlf" },
            TargetLanguage = "fr"
        };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () => 
            await actions.CreateMqmReportInBackground(request)
        );

        // Assert
        StringAssert.Contains(ex.Message, "which is not supported for batch jobs");
    }
}