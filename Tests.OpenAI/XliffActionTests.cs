using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Tests.OpenAI.Base;
using Apps.OpenAI.Models.Requests.Xliff;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;

namespace Tests.OpenAI;

[TestClass]
public class XliffActionTests : TestBase
{   
    [TestMethod]
    public async Task PostEditXLIFF_WithValidXlfFile_ProcessesSuccessfully()
    {
        foreach (var context in InvocationContext)
        {
            // Arrange
            var actions = new DeprecatedXliffActions(context, FileManagementClient);
            var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
            var prompt = "You are a Swiss machine translation post-editor. You edit texts from German into Italian. Your task is to post-edit a translation. You need to take the source segment into account when post-editing the translation. Check each target segment and perform the following tasks:\r\n- Replace the character \"'\" with the character \"’\". Example: write \"l’indice\" instead of \"l'indice\".\r\n- Make sure that the quotation marks \"«»\" are used in the target text. Example: write «I prezzi aumentano» instead of \"I prezzi aumentano\".\r\n- Make sure percentages are expressed with the symbol \"%\". Make sure there aren't spaces between the number and the symbol. Example: write \"10%\" instead of “10 per cento”.\r\n- Perform a grammar and punctuation check. Focus on spelling, gender and number. Ensure internal consistency and fluency. Try to use impersonal formulations when possible.\r\n- Perform a terminology check. The attached glossary contains the terms that need to be strictly applied to the translation, ensuring grammatical correctness, gender, inflections and plurals.\r\nReturn the new .xliff file. ";

            var editRequest = new PostEditXliffRequest
            {
                DisableTagChecks = true,
                File = new FileReference { Name = "contentful.html.xliff" },
                SourceLanguage = "German",
                TargetLanguage = "Italian",

            };
            string? systemMessage = prompt;
            var glossaryRequest = new GlossaryRequest { Glossary = new FileReference { Name = "glossary.tbx" } };

            // Act
            var result = await actions.PostEditXLIFF(modelIdentifier, editRequest, systemMessage, glossaryRequest);

            // Assert
            PrintResult(context, result);
            Assert.IsNotNull(result);
        }
    }

    [TestMethod]
    public async Task TranslateXliff_WithValidTmxFile_ThrowMisconfigurationError()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new DeprecatedXliffActions(context, FileManagementClient);
            var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
            var translateRequest = new TranslateXliffRequest 
            { 
                File = new FileReference { Name = "test.tmx" } 
            };
            string? systemMessage = null;
            var glossaryRequest = new GlossaryRequest();
            
            await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () =>
                await actions.TranslateXliff(modelIdentifier, translateRequest, systemMessage, glossaryRequest)
            );
        }
    }

    [TestMethod]
    public async Task TranslateXliff_WithTxlfFile_ProcessesSuccessfully()
    {
        foreach (var context in InvocationContext)
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
            PrintResult(context, result);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.File.Name.Contains("Markdown entry"));
        }
    }
    
    [TestMethod]
    public async Task PostEditXLIFF_WithTxlfFile_ProcessesSuccessfully()
    {
        foreach (var context in InvocationContext)
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
            Assert.IsTrue(result.File.Name.Contains("test.xlf"));
            PrintResult(context, result);
        }
    }
    
    [TestMethod]
    public async Task ScoreXLIFF_WithValidTxlfFile_ProcessesSuccessfully()
    {
        foreach (var context in InvocationContext)
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
            Assert.IsTrue(result.File.Name.Contains("test"));
            PrintResult(context, result);
        }
    }

    [TestMethod]
    public async Task PromptXLIFF_WithXliffFile_ProcessesSuccessfully()
    {
        foreach (var context in InvocationContext) {
            // Arrange
            var xliffActions = new DeprecatedXliffActions(context, FileManagementClient);
            var model = new TextChatModelIdentifier { ModelId = "gpt-4.1" };
            var request = new PromptXliffRequest { File = new FileReference { Name = "3 random sentences-en-de-T.mxliff" } };
            var prompt = "Get the input list and reply with translations only. Do not modify translations, repply with them to validate connection.";
            var systemPrompt = "You're validating connection to an LLM.";
            var glossary = new GlossaryRequest();
            var baseChatRequest = new BaseChatRequest();

            // Act
            var result = await xliffActions.PromptXLIFF(model, request, prompt, systemPrompt, glossary, baseChatRequest, 5);

            // Assert
            Assert.IsNotNull(result);
            PrintResult(context, result);
        }
    }

    [TestMethod]
    public async Task PromptXLIFF_WithXliffFileAndGlossary_ProcessesSuccessfully()
    {
        foreach (var context in InvocationContext)
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
            PrintResult(context, result);
        }
    }
}
