using Apps.OpenAI.Actions;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests;
using Apps.OpenAI.Models.Requests.Audio;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.OpenAI.Base;

namespace Tests.OpenAI;

[TestClass]
public class AudioServiceTests : TestBase
{
    [TestMethod]
    public async Task CreateTranscription_OpenAi_ReturnsTranscription()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAi);
        var handler = new AudioActions(context, FileManagementClient);
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "tts delorean.mp3" },
            Language = "en",
        };

        // Act
        var result = await handler.CreateTranscription(request);

        // Assert
        Console.WriteLine(result.Transcription);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task CreateTranscription_OpenAiEmbedded_ReturnsTranscription()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.OpenAiEmbedded);
        var handler = new AudioActions(context, FileManagementClient);
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "tts delorean.mp3" },
            Language = "en",
        };

        // Act
        var result = await handler.CreateTranscription(request);

        // Assert
        Console.WriteLine(result.Transcription);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task CreateTranscription_AzureOpenAi_ThrowsMisconfigException()
    {
        // Arrange
        var context = GetInvocationContext(ConnectionTypes.AzureOpenAi);
        var handler = new AudioActions(context, FileManagementClient);
        var request = new TranscriptionRequest
        {
            File = new FileReference { Name = "tts delorean.mp3" },
            Language = "en",
        };

        // Act
        var ex = await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(async () => 
            await handler.CreateTranscription(request)
        );

        // Assert
        StringAssert.Contains(ex.Message, "Azure OpenAI does not support audio actions. Please use OpenAI for such tasks");
    }

    [TestMethod]
    public async Task CreateSpeechAsync()
    {
        foreach (var context in InvocationContext)
        {
            var handler = new AudioActions(context, FileManagementClient);
            var data = await handler.CreateSpeech(new SpeechCreationModelIdentifier { ModelId = "tts-1" },
                new CreateSpeechRequest { InputText = "Hello dear friend! How are you? It`s been a while" });

            Assert.IsNotNull(data);
        }
    }
}
