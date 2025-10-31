using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common.Files;
using Apps.OpenAI.Models.Requests.Background;
using Tests.OpenAI.Base;
using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.OpenAI;

[TestClass]
public class EditTests : TestBase
{
    [TestMethod]
    public async Task Edit_xliff()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new EditActions(context, FileManagementClient);
            var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
            var editRequest = new EditContentRequest
            {
                File = new FileReference { Name = "contentful.html.xlf" },
            };
            var reasoningEffortRequest = new ReasoningEffortRequest
            {
                ReasoningEffort = "low"
            };
            string? systemMessage = null;
            var glossaryRequest = new GlossaryRequest();

            var result = await actions.EditContent(modelIdentifier, editRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.File.Name.Contains("contentful"));
            PrintResult(context, result);
        }
    }

    [TestMethod]
    public async Task Taus_edit()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new EditActions(context, FileManagementClient);
            var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
            var editRequest = new EditContentRequest
            {
                File = new FileReference { Name = "taus.xliff" },
            };
            var reasoningEffortRequest = new ReasoningEffortRequest
            {
                ReasoningEffort = "low"
            };
            string? systemMessage = null;
            var glossaryRequest = new GlossaryRequest();

            var result = await actions.EditContent(modelIdentifier, editRequest, systemMessage, glossaryRequest, reasoningEffortRequest);
            
            Assert.IsNotNull(result);
            PrintResult(context, result);
        }
    }

    [TestMethod]
    public async Task EditInBackground_OpenAiEmbeddedWithXliffFile_Success()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAi);
        var actions = new EditActions(context, FileManagementClient);

        var editRequest = new StartBackgroundProcessRequest
        {
            ModelId = "gpt-4.1",
            File = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" },
            TargetLanguage = "fr"
        };

        // Act
        var response = await actions.EditInBackground(editRequest);

        // Assert          
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.BatchId);
        PrintResult(context, response);
    }

    [TestMethod]
    public async Task EditInBackground_AzureOpenAiWithXliffFile_ThrowsExceptionWithCorrectMessage()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.AzureOpenAi);
        var actions = new EditActions(context, FileManagementClient);

        var editRequest = new StartBackgroundProcessRequest
        {
            File = new FileReference { Name = "The Hobbit, or There and Back Again_en-US.html.xlf" },
            TargetLanguage = "fr"
        };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () =>
            await actions.EditInBackground(editRequest)
        );

        // Assert          
        StringAssert.Contains(ex.Message, "which is not supported for batch jobs");
    }
}
