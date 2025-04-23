using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Tests.OpenAI.Base;
using Apps.OpenAI.Models.Requests.Xliff;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;

namespace Tests.OpenAI;

[TestClass]
public class ChatActionsTests : TestBase
{
    [TestMethod]
    public async Task ChatMessageRequest_WithSimpleTextMessage_ReturnsValidResponse()
    {
        var actions = new ChatActions(InvocationContext, FileManagementClient);
        var result = await actions.ChatMessageRequest(
            new TextChatModelIdentifier { ModelId = "o3-mini" },
            new ChatRequest { Message = "Hello!" },
            new GlossaryRequest());

        Console.WriteLine(result.Message);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod]
    public async Task ChatMessageRequest_WithAudioFile_ReturnsValidResponse()
    {
        var actions = new ChatActions(InvocationContext, FileManagementClient);
        var result = await actions.ChatMessageRequest(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new ChatRequest
            {
                Message = "Answer to the audio file!",
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference 
                { 
                    Name = "tts delorean.mp3", 
                    ContentType = "audio/mp3" 
                }
            },
            new GlossaryRequest());

        Console.WriteLine(result.Message);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod]
    public async Task PostEditXLIFF_WithValidXlfFile_ProcessesSuccessfully()
    {
        var actions = new ChatActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var editRequest = new PostEditXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.xlf" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();
        
        var result = await actions.PostEditXLIFF(modelIdentifier, editRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("test"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task TranslateXliff_WithValidTmxFile_ThrowMisconfigurationError()
    {
        var actions = new ChatActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var translateRequest = new TranslateXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.tmx" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();
        
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () =>
        {
            await actions.TranslateXliff(modelIdentifier, translateRequest, systemMessage, glossaryRequest);
        });
    }

    [TestMethod]
    public async Task TranslateXliff_WithTxlfFile_ProcessesSuccessfully()
    {
        var actions = new ChatActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var translateRequest = new TranslateXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();
        
        var result = await actions.TranslateXliff(modelIdentifier, translateRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("Markdown entry"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    
    [TestMethod]
    public async Task PostEditXLIFF_WithTxlfFile_ProcessesSuccessfully()
    {
        var actions = new ChatActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var editRequest = new PostEditXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.xlf" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest()
        {
            Glossary = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "glossary.tbx" } 
        };
        
        var result = await actions.PostEditXLIFF(modelIdentifier, editRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("test.xlf"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
    
    [TestMethod]
    public async Task ScoreXLIFF_WithValidTxlfFile_ProcessesSuccessfully()
    {
        var actions = new ChatActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var scoreRequest = new ScoreXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "test.xlf" },
            Threshold = new []{ 8.0},
            Condition = new []{">="},
            State = new []{"needs-adaptation"}
        };
        
        var result = await actions.ScoreXLIFF(modelIdentifier, scoreRequest, null, 1500);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("test"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    [TestMethod]
    public async Task LocalizeText_WithSerbianLocale_ReturnsLocalizedText()
    {
        var actions = new ChatActions(InvocationContext, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var localizeRequest = new LocalizeTextRequest 
        { 
            Text = "Develop and implement an HR strategy that drives organizational productivity and supports company's business goals. Design and oversee processes that promote team efficiency and operational effectiveness while reducing complexity and redundancies.",
            Locale = "sr-Latn-RS"
        };

        var glossaryRequest = new GlossaryRequest();
        
        var result = await actions.LocalizeText(modelIdentifier, localizeRequest, glossaryRequest);
        
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Message);
        Console.WriteLine("Original: " + localizeRequest.Text);
        Console.WriteLine("Localized: " + result.Message);
        
        // Additional validation to ensure response is not empty and contains Serbian characters
        Assert.IsTrue(result.Message.Length > 0);
    }
}
