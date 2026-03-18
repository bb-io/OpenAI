using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Requests.Content;
using Blackbird.Applications.Sdk.Common.Files;
using Apps.OpenAI.Models.Requests.Background;
using Tests.OpenAI.Base;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Apps.OpenAI.Constants;

namespace Tests.OpenAI;

[TestClass]
public class EditTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task Edit_xliff(InvocationContext context)
    {
        var actions = new EditActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var editRequest = new EditContentRequest
        {
            File = new FileReference { Name = "GUID-99E95005-E212-481D-AEBC-67DFA3BD38E8_1_en-US-en-zh_cn-Tr.mxliff" },
            OutputFileHandling = "xliff1",
            ProcessOnlySegmentState = "Initial",
            ModifiedBy = "1441948"
        };
        var reasoningEffortRequest = new ReasoningEffortRequest
        {
            //ReasoningEffort = "low"
        };
        string? systemMessage = "Your task is to post-edit translation segments by correcting critical errors, comparing each target to its source. Critical errors include tag misplacements, malformed tags, number mismatches, translation omissions, or glossary term violations.  Tags appear as combinations of {, }, <, or > with a number (e.g., {1}, <2}, {3>), and these must match the source exactly. Tags define font styles of texts between two tags or represent inserted links and line breaks. \nDo not revert any translation to English.\nDo no change translation style. ";
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.EditContent(modelIdentifier, editRequest, systemMessage, glossaryRequest, reasoningEffortRequest);

        Assert.IsNotNull(result);
        //Assert.Contains("contentful", result.File.Name);
        PrintResult(result);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task Taus_edit(InvocationContext context)
    {
        var actions = new EditActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var editRequest = new EditContentRequest
        {
            File = new FileReference { Name = "taus.xliff", ContentType = "application/vnd.oasis.xliff+xml" },
        };
        var reasoningEffortRequest = new ReasoningEffortRequest
        {
            ReasoningEffort = "low"
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        var result = await actions.EditContent(modelIdentifier, editRequest, systemMessage, glossaryRequest, reasoningEffortRequest);

        Assert.IsNotNull(result);
        PrintResult(result);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task EditInBackground_OpenAiWithXliffFile_Success(InvocationContext context)
    {
        // Arrange
        var actions = new EditActions(context, FileManagementClient);
        var file = new FileReference 
        { 
            Name = "The Hobbit, or There and Back Again_en-US.html.xlf", 
            ContentType = "application/vnd.oasis.xliff+xml" 
        };

        var editRequest = new StartBackgroundProcessRequest
        {
            ModelId = "gpt-5-mini",
            File = file,
            TargetLanguage = "fr",
            //MaximumTokens = 4096
        };

        // Act
        var response = await actions.EditInBackground(editRequest);

        // Assert          
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.BatchId);
        PrintResult(response);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task EditInBackground_AzureOpenAiWithXliffFile_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var actions = new EditActions(context, FileManagementClient);
        var file = new FileReference 
        { 
            Name = "The Hobbit, or There and Back Again_en-US.html.xlf", 
            ContentType = "application/x-xliff+xml" 
        };

        var editRequest = new StartBackgroundProcessRequest
        {
            File = file,
            TargetLanguage = "fr"
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () =>
            await actions.EditInBackground(editRequest)
        );

        // Assert          
        StringAssert.Contains(ex.Message, "which is not supported for batch jobs");
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task Prompt_Generates_FullyCustomPrompt(InvocationContext context)
    {
        var actions = new EditActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5-mini" };
        var file = new FileReference { Name = "mqm-min.xlf", ContentType = "application/x-xliff+xml" };
        var editRequest = new EditContentRequest { File = file };
        var systemPrompt = "Reply with json array of objects for each traslation unit, repeating ID and setting Target to one";
        var glossaryRequest = new GlossaryRequest();
        var reasoningEffortRequest = new ReasoningEffortRequest();

        var result = await actions.Prompt(
            modelIdentifier,
            editRequest,
            systemPrompt,
            glossaryRequest,
            reasoningEffortRequest);

        PrintResult(result);
        Assert.Contains("mqm", result.File.Name);
    }
}
