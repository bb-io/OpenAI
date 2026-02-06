using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class ChatActionsTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task ChatMessageRequest_OpenAiEmbeddedWithSimpleTextMessage_ReturnsValidResponse(InvocationContext context)
    {
        // Arrange
        var actions = new ChatActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = "gpt-4" };
        var chatRequest = new ChatRequest
        {
            Message = "Tell me about Scania S",
            MaximumTokens = 300
        };
        var glossary = new GlossaryRequest();

        // Act
        var result = await actions.ChatMessageRequest(model, chatRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task ChatMessageRequest_AzureOpenAiWithSimpleTextMessage_ReturnsValidResponse(InvocationContext context)
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
        PrintResult(result);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod, ContextDataSource]
    public async Task ChatMessageRequest_WithHtmlFile_ReturnsValidResponse(InvocationContext context)
    {
        // Arrange
        var actions = new ChatActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-5" };
        var chatRequest = new ChatRequest
        {
            Message = "Give a couple of SEO keywords",
            File = new FileReference
            {
                Name = "contentful.html",
                ContentType = "text/html"
            }
        };
        var glossary = new GlossaryRequest();

        // Act
        var result = await actions.ChatMessageRequest(modelIdentifier, chatRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task ChatMessageRequest_OpenAiEmbeddedWithAudioFile_ReturnsValidResponse(InvocationContext context)
    {
        // Arrange
        var actions = new ChatActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var chatRequest = new ChatRequest
        {
            Message = "Answer to the audio file!",
            File = new FileReference
            {
                Name = "tts delorean.mp3",
                ContentType = "audio/mp3"
            }
        };
        var glossary = new GlossaryRequest();

        // Act
        var result = await actions.ChatMessageRequest(modelIdentifier, chatRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task ChatMessageRequest_OpenAiWithSimpleTextMessageWithoutModelIdentifier_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var actions = new ChatActions(context, FileManagementClient);
        var model = new TextChatModelIdentifier { ModelId = null };
        var chatRequest = new ChatRequest
        {
            Message = "Who are you? State your model, creator, and your main responsibilities."
        };
        var glossary = new GlossaryRequest();

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () =>
            await actions.ChatMessageRequest(model, chatRequest, glossary)
        );

        // Assert
        Assert.Contains("Please select a model to execute this action using the OpenAI connection", ex.Message);
    }
    
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task ChatMessageRequest_OpenAiEmbeddedWithSimpleTextMessageWithoutModelIdentifier_ReturnsValidResponse(InvocationContext context)
    {
        // Arrange
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
        PrintResult(result);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded)]
    public async Task ChatMessageRequest_OpenAiEmbeddedWithSimpleTextMessageWithModelIdentifier_ReturnsValidResponse(InvocationContext context)
    {
        // Arrange
        var actions = new ChatActions(context, FileManagementClient);
        var modelId = new TextChatModelIdentifier { ModelId = "gpt-5" };
        var chatRequest = new ChatRequest
        {
            Message = "Who are you? State your model, creator, and your main responsibilities."
        };
        var glossary = new GlossaryRequest();

        // Act
        var result = await actions.ChatMessageRequest(modelId, chatRequest, glossary);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task ChatMessageRequest_AzureOpenAiWithAudioFile_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var actions = new ChatActions(context, FileManagementClient);
        var modelIdentifier = new TextChatModelIdentifier { ModelId = "gpt-4o" };
        var chatRequest = new ChatRequest
        {
            Message = "Answer to the audio file!",
            File = new FileReference
            {
                Name = "tts delorean.mp3",
                ContentType = "audio/mp3"
            }
        };
        var glossary = new GlossaryRequest();

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () => 
            await actions.ChatMessageRequest(modelIdentifier, chatRequest, glossary)
        );

        // Assert
        Assert.Contains("Azure OpenAI does not support chat actions with audio files", ex.Message);
    }
}
