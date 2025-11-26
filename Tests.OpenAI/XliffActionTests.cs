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
    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task PostEditXLIFF_WithValidXlfFile_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var actions = new DeprecatedXliffActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { /*ModelId = "gpt-4.1" */};
        var prompt = "";
        var editRequest = new PostEditXliffRequest
        {
            //DisableTagChecks = true,
            File = new FileReference { Name = "Pasted text-en-ko-Tr.mxliff" },
            //SourceLanguage = "German",
            //TargetLanguage = "Italian",
            NeverFail = false,
            ModifiedBy= "1441948",
            FilterGlossary=true,
            PostEditLockedSegments = false
        };
        string? systemMessage = prompt;
        var glossaryRequest = new GlossaryRequest { Glossary = new FileReference { Name = "TermWeb_Safe For AI.tbx" } };

        // Act
        var result = await actions.PostEditXLIFF(modelIdentifier, editRequest, systemMessage, glossaryRequest);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, ContextDataSource]
    public async Task TranslateXliff_WithValidTmxFile_ThrowMisconfigurationError(InvocationContext context)
{
        var actions = new DeprecatedXliffActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var translateRequest = new TranslateXliffRequest 
        { 
            File = new FileReference { Name = "test.tmx" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () =>
            await actions.TranslateXliff(modelIdentifier, translateRequest, systemMessage, glossaryRequest)
        );
    }

    [TestMethod, ContextDataSource]
    public async Task TranslateXliff_WithTxlfFile_ProcessesSuccessfully(InvocationContext context)
    {
        // Arrange
        var actions = new DeprecatedXliffActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var translateRequest = new TranslateXliffRequest
        {
            File = new FileReference { Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf" }
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();

        // Act
        var result = await actions.TranslateXliff(modelIdentifier, translateRequest, systemMessage, glossaryRequest);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.Contains("Markdown entry", result.File.Name);
    }
    
    [TestMethod, ContextDataSource]
    public async Task PostEditXLIFF_WithTxlfFile_ProcessesSuccessfully(InvocationContext context)
    {
        var actions = new DeprecatedXliffActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var editRequest = new PostEditXliffRequest 
        { 
            File = new FileReference { Name = "test.xlf" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest()
        {
            Glossary = new FileReference { Name = "glossary.tbx" } 
        };
            
        var result = await actions.PostEditXLIFF(modelIdentifier, editRequest, systemMessage, glossaryRequest);
            
        Assert.IsNotNull(result);
        Assert.Contains("test.xlf", result.File.Name);
        PrintResult(result);
    }
    
    [TestMethod, ContextDataSource]
    public async Task ScoreXLIFF_WithValidTxlfFile_ProcessesSuccessfully(InvocationContext context)
    {
        var actions = new DeprecatedXliffActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var scoreRequest = new ScoreXliffRequest 
        { 
            File = new FileReference { Name = "test.xlf" },
            Threshold = new []{ 8.0},
            Condition = new []{">="},
            State = new []{"needs-adaptation"}
        };
            
        var result = await actions.ScoreXLIFF(modelIdentifier, scoreRequest, null, 1500);

        Assert.IsNotNull(result);
        Assert.Contains("test", result.File.Name);
        PrintResult(result);
    }

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
