using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Requests.Audio;
using Apps.OpenAI.Models.Responses.Audio;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.OpenAI.Actions;

[ActionList]
public class AudioActions : BaseActions
{
    public AudioActions(InvocationContext invocationContext) : base(invocationContext) { }

    [Action("Create English translation", Description = "Generates a translation into English given an audio or " +
                                                        "video file (mp3, mp4, mpeg, mpga, m4a, wav, or webm).")]
    public async Task<TranslationResponse> CreateTranslation([ActionParameter] TranslationRequest input)
    {
        var request = new OpenAIRequest("/audio/translations", Method.Post, Creds);
        request.AddFile("file", input.File.Bytes, input.File.Name);
        request.AddParameter("model", "whisper-1");
        request.AddParameter("response_format", "verbose_json");
        request.AddParameter("temperature", input.Temperature ?? 0);

        var response = await Client.ExecuteWithErrorHandling<TextDto>(request);
        return new()
        {
            TranslatedText = response.Text
        };
    }

    [Action("Create transcription", Description = "Generates a transcription given an audio or video file (mp3, " +
                                                  "mp4, mpeg, mpga, m4a, wav, or webm).")]
    public async Task<TranscriptionResponse> CreateTranscription([ActionParameter] TranscriptionRequest input)
    {
        var request = new OpenAIRequest("/audio/transcriptions", Method.Post, Creds);
        request.AddFile("file", input.File.Bytes, input.File.Name);
        request.AddParameter("model", "whisper-1");
        request.AddParameter("response_format", "verbose_json");
        request.AddParameter("temperature", input.Temperature ?? 0);
        request.AddParameter("language", input.Language);

        var response = await Client.ExecuteWithErrorHandling<TextDto>(request);
        return new()
        {
            Transcription = response.Text
        };
    }
}