using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Requests.Background;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ReportingActionsTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource]
    public async Task CreateMqmReportInBackground_OpenAiEmbeddedXliffFile_Success(InvocationContext context)
    {
        // Arrange
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
        PrintResult(result);
    }

    [TestMethod, ContextDataSource]
    public async Task CreateMqmReportInBackground_AzureOpenAiXliffFile_Success(InvocationContext context)
    {
        // Arrange
        var actions = new ReportingActions(context, FileManagementClient);
        var request = new CreateMqmReportInBackgroundRequest()
        {
            ModelId = "gpt-4.1",
            File = new FileReference { Name = "mqm.xlf" },
            TargetLanguage = "fr"
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () => 
            await actions.CreateMqmReportInBackground(request)
        );

        // Assert
        Assert.Contains("which is not supported for batch jobs", ex.Message);
    }
}