using System.Threading.Tasks;
using Apps.OpenAI.Extensions;
using Apps.OpenAI.Invocables;
using Apps.OpenAI.Models.Requests.Audio;
using Apps.OpenAI.Models.Responses.Audio;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;

namespace Apps.OpenAI.Actions;

[ActionList]
public class AudioActions : OpenAiInvocable
{
    private IOpenAIService Client { get; }

    public AudioActions(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = Creds.CreateOpenAiServiceSdk();
    }

    [Action("Create English translation", Description = "Generates a translation into English given an audio or " +
                                                        "video file (mp3, mp4, mpeg, mpga, m4a, wav, or webm).")]
    public async Task<TranslationResponse> CreateTranslation([ActionParameter] TranslationRequest input)
    {
        var audioResult = await Client.Audio.CreateTranslation(new()
        {
            FileName = input.FileName ?? input.File.Name,
            File = input.File.Bytes,
            Model = "whisper-1",
            ResponseFormat = StaticValues.AudioStatics.ResponseFormat.VerboseJson,
            Temperature = input.Temperature
        });
        audioResult.ThrowOnError();

        return new()
        {
            TranslatedText = audioResult.Text
        };
    }

    [Action("Create transcription", Description = "Generates a transcription given an audio or video file. ( mp3, " +
                                                  "mp4, mpeg, mpga, m4a, wav, or webm)")]
    public async Task<TranscriptionResponse> CreateTranscription([ActionParameter] TranscriptionRequest input)
    {
        var audioResult = await Client.Audio.CreateTranscription(new()
        {
            FileName = input.FileName ?? input.File.Name,
            File = input.File.Bytes,
            Model = "whisper-1",
            ResponseFormat = StaticValues.AudioStatics.ResponseFormat.VerboseJson,
            Language = input.Language,
            Temperature = input.Temperature,
        });
        audioResult.ThrowOnError();

        return new()
        {
            Transcription = audioResult.Text
        };
    }
}