using Tests.OpenAI.Base;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests;
using Apps.OpenAI.Models.Requests.Audio;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.OpenAI;

[TestClass]
public class AudioServiceTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded, ConnectionTypes.OpenAi)]
    public async Task CreateTranscription_OpenAi_ReturnsTranscription(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var request = new TranscriptionRequest
        {
            Model = "whisper-1",
            File = new FileReference { Name = "tts delorean.mp3" },
            Language = "en",
        };

        // Act
        var result = await handler.CreateTranscription(request);

        // Assert
        Console.WriteLine(result.Transcription);
        Assert.IsNotNull(result);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task CreateTranscription_AzureOpenAi_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var request = new TranscriptionRequest
        {
            Model = "whisper-1",
            File = new FileReference { Name = "tts delorean.mp3" },
            Language = "en",
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () => 
            await handler.CreateTranscription(request)
        );

        // Assert
        Assert.Contains("Azure OpenAI does not support audio actions. Please use OpenAI for such tasks", ex.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded, ConnectionTypes.OpenAi)]
    public async Task CreateTranscription_OpenAi_Prompt_WithGpt4oTranscribeDiarize_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var request = new TranscriptionRequest
        {
            Model = "gpt-4o-transcribe-diarize",
            File = new FileReference { Name = "tts delorean.mp3" },
            Prompt = "Some prompt",
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () =>
            await handler.CreateTranscription(request)
        );

        // Assert
        Assert.Contains("Prompt parameter is not supported when using the 'gpt-4o-transcribe-diarize' model.", ex.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded, ConnectionTypes.OpenAi)]
    public async Task CreateTranscription_OpenAi_SpeakerNames_MoreThanFour_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var request = new TranscriptionRequest
        {
            Model = "whisper-1",
            File = new FileReference { Name = "tts delorean.mp3" },
            KnownSpeakerNames = new List<string> { "Speaker 1", "Speaker 2", "Speaker 3", "Speaker 4", "Speaker 5" }
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () =>
            await handler.CreateTranscription(request)
        );

        // Assert
        Assert.Contains("Known speaker names parameter supports a maximum of 4 names.", ex.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAi)]
    public async Task CreateSpeech_OpenAi_ReturnsSpeech(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var model = new SpeechCreationModelIdentifier { ModelId = "tts-1" };
        var request = new CreateSpeechRequest { InputText = "Hello dear friend! How are you? It`s been a while" };

        // Act
        var data = await handler.CreateSpeech(model, request);

        // Assert
        Assert.IsNotNull(data);
    }
}
