using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests;
using Apps.OpenAI.Models.Requests.Audio;
using Apps.OpenAI.Models.Responses.Audio;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.OpenAI.Actions;

[ActionList]
public class AudioActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Create English translation", Description = "Generates a translation into English given an audio or " +
                                                        "video file (mp3, mp4, mpeg, mpga, m4a, wav, or webm).")]
    public async Task<TranslationResponse> CreateTranslation([ActionParameter] TranslationRequest input)
    {
        var request = new OpenAIRequest("/audio/translations", Method.Post);
        var fileStream = await FileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        request.AddFile("file", fileBytes, input.File.Name);
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
        var request = new OpenAIRequest("/audio/transcriptions", Method.Post);
        var fileStream = await FileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        request.AddFile("file", fileBytes, input.File.Name);
        request.AddParameter("model", "whisper-1");
        request.AddParameter("response_format", "verbose_json");
        request.AddParameter("temperature", input.Temperature ?? 0);
        request.AddParameter("language", input.Language);
        
        if (input.TimestampGranularities != null && input.TimestampGranularities.Any())
        {
            foreach (var granularity in input.TimestampGranularities)
            {
                request.AddParameter("timestamp_granularities[]", granularity);
            }
        }
        
        var response = await Client.ExecuteWithErrorHandling<TranscriptionDto>(request);
        var words = response.Words?.Select(x => new WordResponse(x)).ToList() ?? new List<WordResponse>();
        var segments = response.Segments?.Select(x => new SegmentResponse(x)).ToList() ?? new List<SegmentResponse>();
        
        return new()
        {
            Transcription = response.Text,
            Words = JsonConvert.SerializeObject(words),
            Segments = JsonConvert.SerializeObject(segments)
        };
    }

    [Action("Create speech", Description = "Generates audio from the text input.")]
    public async Task<CreateSpeechResponse> CreateSpeech(
        [ActionParameter] SpeechCreationModelIdentifier modelIdentifier,
        [ActionParameter] CreateSpeechRequest input)
    {
        var model = modelIdentifier.ModelId ?? "tts-1-hd";
        var responseFormat = input.ResponseFormat ?? "mp3";

        var request = new OpenAIRequest("/audio/speech", Method.Post);
        request.AddJsonBody(new
        {
            model,
            input = input.InputText,
            voice = input.Voice,
            response_format = responseFormat,
            speed = input.Speed ?? 1.0f
        });

        var response = await Client.ExecuteWithErrorHandling(request);

        using var stream = new MemoryStream(response.RawBytes);
        var file = await FileManagementClient.UploadAsync(stream, response.ContentType,
            $"{input.OutputAudioName ?? input.Voice}.{responseFormat}");

        return new(file);
    }
}