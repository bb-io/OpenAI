using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Tests.OpenAI.Base;
using Apps.OpenAI.Models.Requests.Xliff;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Apps.OpenAI.Constants;

namespace Tests.OpenAI;

[TestClass]
public class XliffActionTests : TestBaseWithContext
{   
    [TestMethod, ContextDataSource]
    public async Task PromptXLIFF_WithXliffFile_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var xliffActions = new DeprecatedXliffActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var request = new PromptXliffRequest 
        { 
            File = new FileReference { Name = "3 random sentences-en-de-T.mxliff" },
            ModifiedBy = "Test"
        };

        var systemPrompt = "You're validating connection to an LLM.";
        var prompt = "Get the input list and reply with translations only. Do not modify translations, repply with them to validate connection.";

        var glossary = new GlossaryRequest();
        var baseChatRequest = new BaseChatRequest();

        // Act
        var result = await xliffActions.PromptXLIFF(model, request, prompt, systemPrompt, glossary, baseChatRequest, 5);

        // Assert
        Assert.IsNotNull(result);
        PrintResult(result);
    }

    [TestMethod, ContextDataSource]
    public async Task PromptXLIFF_WithXliffFileAndGlossary_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var xliffActions = new DeprecatedXliffActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
        var request = new PromptXliffRequest { File = new FileReference { Name = "3 random sentences-en-de-T.mxliff" } };
        var prompt = "Get the input list and reply with translations only. Do not modify translations, repply with them to validate connection.";
        var systemPrompt = "You're validating connection to an LLM.";
        var glossary = new GlossaryRequest { Glossary = new FileReference { Name = "glossary.tbx" } };
        var baseChatRequest = new BaseChatRequest();

        // Act
        var result = await xliffActions.PromptXLIFF(model, request, prompt, systemPrompt, glossary, baseChatRequest, 5);

        // Assert
        Assert.IsNotNull(result);
        PrintResult(result);
    }
}
