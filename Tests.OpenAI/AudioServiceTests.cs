using Apps.OpenAI.Actions;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests;
using Apps.OpenAI.Models.Requests.Audio;
using Blackbird.Applications.Sdk.Common.Files;
using Tests.OpenAI.Base;

namespace Tests.OpenAI
{
    [TestClass]
    public class AudioServiceTests : TestBase
    {
        [TestMethod]
        public async Task CreateTranscriptionAsync()
        {
            var handler = new AudioActions(InvocationContext, FileManagementClient);
            var data = await handler.CreateTranscription(
                new TranscriptionRequest
                {
                    File = new FileReference { Name = "tts delorean.mp3" },

                    Language = "en",
                });

            Console.WriteLine(data.Transcription);
            Assert.IsNotNull(data);
        }

        [TestMethod]
        public async Task CreateSpeechAsync()
        {
            var handler = new AudioActions(InvocationContext, FileManagementClient);
            var data = await handler.CreateSpeech(new SpeechCreationModelIdentifier { ModelId = "tts-1" },
                new CreateSpeechRequest { InputText = "Hello dear friend! How are you? It`s been a while" });

            Assert.IsNotNull(data);
        }
    }
}
