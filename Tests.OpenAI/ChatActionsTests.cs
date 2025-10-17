using Newtonsoft.Json;
using Tests.OpenAI.Base;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Chat;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.OpenAI;

[TestClass]
public class ChatActionsTests : TestBase
{
    [TestMethod]
    public async Task ChatMessageRequest_WithSimpleTextMessage_ReturnsValidResponse()
    {
        foreach (var context in InvocationContext)
        {
            // Arrange
            var actions = new ChatActions(context, FileManagementClient);
            var model = new TextChatModelIdentifier { ModelId = "gpt-5" };
            var chatRequest = new ChatRequest
            {
                Message = "Who are you? State your model, creator, and your main responsibilities."
            };
            var glossary = new GlossaryRequest();

            // Act
            var result = await actions.ChatMessageRequest(model, chatRequest, glossary);

            // Assert
            PrintResult(context, result);
            Assert.IsNotNull(result.Message);
        }
    }

    [TestMethod]
    public async Task ChatMessageRequest_AzureOpenAiWithSimpleTextMessage_ReturnsValidResponse()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.AzureOpenAi);
        var actions = new ChatActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = null };
        var chatRequest = new ChatRequest
        {
            Message = "Who are you? State your model, creator, and your main responsibilities."
        };
        var glossary = new GlossaryRequest();

        // Act
        var result = await actions.ChatMessageRequest(model, chatRequest, glossary);

        // Assert
        PrintResult(context, result);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod]
    public async Task ChatMessageRequest_WithHtmlFile_ReturnsValidResponse()
    {
        foreach (var context in InvocationContext)
        {
            // Arrange
            var actions = new ChatActions(context, FileManagementClient);
            var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5" };
            var chatRequest = new ChatRequest
            {
                Message = "Give a couple of SEO keywords",
                File = new Blackbird.Applications.Sdk.Common.Files.FileReference
                {
                    Name = "contentful.html",
                    ContentType = "text/html"
                }
            };
            var glossary = new GlossaryRequest();

            // Act
            var result = await actions.ChatMessageRequest(modelIdentifier, chatRequest, glossary);

            // Assert
            PrintResult(context, result);
            Assert.IsNotNull(result.Message);
        }
    }

    [TestMethod]
    public async Task ChatMessageRequest_OpenAIWithAudioFile_ReturnsValidResponse()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAi);
        var actions = new ChatActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var chatRequest = new ChatRequest
        {
            Message = "Answer to the audio file!",
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference
            {
                Name = "tts delorean.mp3",
                ContentType = "audio/mp3"
            }
        };
        var glossary = new GlossaryRequest();

        // Act
        var result = await actions.ChatMessageRequest(modelIdentifier, chatRequest, glossary);

        // Assert
        PrintResult(context, result);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod]
    public async Task ChatMessageRequest_OpenAiWithSimpleTextMessageWithoutModelIdentifier_ThrowsMisconfigException()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAi);
        var actions = new ChatActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = null };
        var chatRequest = new ChatRequest
        {
            Message = "Who are you? State your model, creator, and your main responsibilities."
        };
        var glossary = new GlossaryRequest();

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () =>
            await actions.ChatMessageRequest(model, chatRequest, glossary)
        );

        // Assert
        StringAssert.Contains(ex.Message, "Please select a model to execute this action using the OpenAI connection");
    }

    [TestMethod]
    public async Task ChatMessageRequest_AzureOpenAIWithAudioFile_ThrowsMisconfigException()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.AzureOpenAi);
        var actions = new ChatActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var chatRequest = new ChatRequest
        {
            Message = "Answer to the audio file!",
            File = new Blackbird.Applications.Sdk.Common.Files.FileReference
            {
                Name = "tts delorean.mp3",
                ContentType = "audio/mp3"
            }
        };
        var glossary = new GlossaryRequest();

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () => 
            await actions.ChatMessageRequest(modelIdentifier, chatRequest, glossary)
        );

        // Assert
        StringAssert.Contains(ex.Message, "Azure OpenAI does not support chat actions with audio files");
    }

    private static void PrintResult(InvocationContext context, ChatResponse result)
    {
        Console.WriteLine(context.AuthenticationCredentialsProviders.First(x => x.KeyName == CredNames.ConnectionType).Value);
        Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
        Console.WriteLine();
    }
}
