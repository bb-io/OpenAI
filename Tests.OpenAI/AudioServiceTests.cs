using Tests.OpenAI.Base;
using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests;
using Apps.OpenAI.Models.Requests.Audio;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Tests.OpenAI;

[TestClass]
public class AudioServiceTests : TestBaseWithContext
{
    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded, ConnectionTypes.OpenAi)]
    public async Task CreateTranscription_OpenAi_ReturnsTranscription_DiarizedJsonFormat(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var model = new AudioModelIdentifier { ModelId = "gpt-4o-transcribe-diarize" };
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "Transcription sample short.mp3" },
            Language = "pt",
        };

        // Act
        var result = await handler.CreateTranscription(model, request);
        var segments = JArray.Parse(result.Segments);

        // Assert
        TestContext.WriteLine(result.Transcription);
        TestContext.WriteLine(result.Segments);
        Assert.IsNotNull(result);
        Assert.IsTrue(segments.Count > 0);
        Assert.IsTrue(segments.Any(x => x["Speaker"] != null));
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded, ConnectionTypes.OpenAi)]
    public async Task CreateTranscription_OpenAi_DiarizedModel_AssemblesTranscriptionBySpeaker(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var model = new AudioModelIdentifier { ModelId = "gpt-4o-transcribe-diarize" };
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "Transcription sample short.mp3" },
            Language = "pt",
        };

        // Act
        var result = await handler.CreateTranscription(model, request);

        // Assert
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        Assert.IsNotNull(result);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded, ConnectionTypes.OpenAi)]
    public async Task CreateTranscription_OpenAi_StandardModel_ReturnsSingleBlobText(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var model = new AudioModelIdentifier { ModelId = "gpt-4o-transcribe" };
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "Transcription sample short.mp3" },
            Language = "pt",
        };

        // Act
        var result = await handler.CreateTranscription(model, request);

        // Assert
        TestContext.WriteLine(result.Transcription);
        TestContext.WriteLine(result.Segments);
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Transcription.Contains("A:"));
    }

    [TestMethod, ContextDataSource(ConnectionTypes.AzureOpenAi)]
    public async Task CreateTranscription_AzureOpenAi_ReturnsTranscription(InvocationContext context)
    {
        // Arrange
        var actions = new AudioActions(context, FileManagementClient);
        var model = new AudioModelIdentifier { };
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "test.mp3" },
        };

        // Act
        var result = await actions.CreateTranscription(model, request);

        // Assert
        TestContext.WriteLine(result.Transcription);
        TestContext.WriteLine(result.Segments);
        Assert.IsNotNull(result);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded, ConnectionTypes.OpenAi)]
    public async Task CreateTranscription_OpenAi_Prompt_WithGpt4oTranscribeDiarize_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var model = new AudioModelIdentifier { ModelId = "gpt-4o-transcribe-diarize" };
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "Transcription sample short.mp3" },
            Prompt = "Some prompt",
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () =>
            await handler.CreateTranscription(model, request)
        );

        // Assert
        Assert.Contains("Prompt parameter is not supported when using the 'gpt-4o-transcribe-diarize' model.", ex.Message);
    }

    [TestMethod, ContextDataSource(ConnectionTypes.OpenAiEmbedded, ConnectionTypes.OpenAi)]
    public async Task CreateTranscription_OpenAi_TimestampGranularities_WithoutWhisper1Model_ThrowsMisconfigException(InvocationContext context)
    {
        // Arrange
        var handler = new AudioActions(context, FileManagementClient);
        var model = new AudioModelIdentifier { ModelId = "gpt-4o-transcribe-diarize" };
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "Transcription sample short.mp3" },
            TimestampGranularities = ["word"]
        };

        // Act
        var ex = await Assert.ThrowsExactlyAsync<PluginMisconfigurationException>(async () =>
            await handler.CreateTranscription(model, request)
        );

        // Assert
        Assert.Contains("Timestamp granularities are only supported when using the 'whisper-1' model.", ex.Message);
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
