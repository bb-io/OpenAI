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
        foreach (var context in InvocationContext)
        {
            var actions = new ChatActions(context, FileManagementClient);
            var result = await actions.ChatMessageRequest(
                new TextChatModelIdentifier 
                { 
                    ModelId = "gpt-5"
                },
                new ChatRequest
                {
                    Message = "Who are you? State your model (are you GPT5?), creator, and your main responsibilities."
                },
                new GlossaryRequest());

            Console.WriteLine(result.Message);
            Assert.IsNotNull(result.Message);
        }
    }

    [TestMethod]
    public async Task ChatMessageRequest_WithHtmlFile_ReturnsValidResponse()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new ChatActions(context, FileManagementClient);
            var result = await actions.ChatMessageRequest(
                new TextChatModelIdentifier
                {
                    ModelId = "gpt-5"
                },
                new ChatRequest
                {
                    Message = "Give a couple of SEO keywords",
                    File = new Blackbird.Applications.Sdk.Common.Files.FileReference { 
                        Name = "contentful.html",
                        ContentType = "text/html"
                    }
                },
                new GlossaryRequest());

            Console.WriteLine(result.Message);
            Assert.IsNotNull(result.Message);
        }
    }

    [TestMethod]
    public async Task ChatMessageRequest_WithAudioFile_ReturnsValidResponse()
    {
        foreach (var context in InvocationContext)
        {
            var actions = new ChatActions(context, FileManagementClient);
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
    }
}
