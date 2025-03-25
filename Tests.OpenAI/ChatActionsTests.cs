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
        var actions = new ChatActions(InvocationContext, FileManager);
        var result = await actions.ChatMessageRequest(
            new TextChatModelIdentifier { ModelId = "gpt-4o" },
            new ChatRequest { Message = "Hello!" },
            new GlossaryRequest());

        Console.WriteLine(result.Message);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod]
    public async Task ChatMessageRequest_WithAudioFile_ReturnsValidResponse()
    {
        var actions = new ChatActions(InvocationContext, FileManager);
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
        var actions = new ChatActions(InvocationContext, FileManager);
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
        var actions = new ChatActions(InvocationContext, FileManager);
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
        var actions = new ChatActions(InvocationContext, FileManager);
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
        var actions = new ChatActions(InvocationContext, FileManager);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o-mini" };
        var editRequest = new PostEditXliffRequest 
        { 
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference { Name = "Markdown entry #1_en-US-Default_HTML-nl-NL#TR_FQTF#.html.txlf" } 
        };
        string? systemMessage = null;
        var glossaryRequest = new GlossaryRequest();
        
        var result = await actions.PostEditXLIFF(modelIdentifier, editRequest, systemMessage, glossaryRequest);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.File.Name.Contains("Markdown entry"));

        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }
}
