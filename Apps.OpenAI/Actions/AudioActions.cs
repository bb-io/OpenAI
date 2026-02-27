using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests;
using Apps.OpenAI.Models.Requests.Audio;
using Apps.OpenAI.Models.Responses.Audio;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Apps.OpenAI.Actions;

[ActionList("Audio")]
public class AudioActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Create English translation", Description = "Generates a translation into English given an audio or " +
                                                        "video file (mp3, mp4, mpeg, mpga, m4a, wav, or webm).")]
    public async Task<TranslationResponse> CreateTranslation([ActionParameter] TranslationRequest input)
    {
        ThrowForAzure("audio");
        var request = new OpenAIRequest("/audio/translations", Method.Post);
        var fileStream = await FileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        request.AddFile("file", fileBytes, input.File.Name);
        request.AddParameter("model", "whisper-1");
        request.AddParameter("response_format", "verbose_json");
        request.AddParameter("temperature", input.Temperature ?? 0);

        var response = await UniversalClient.ExecuteWithErrorHandling<TextDto>(request);
        return new() { TranslatedText = response.Text };
    }

    [Action("Create transcription", Description = "Generates a transcription given an audio or video file (mp3, " +
                                                  "mp4, mpeg, mpga, m4a, wav, or webm).")]
    public async Task<TranscriptionResponse> CreateTranscription([ActionParameter] TranscriptionRequest input)
    {
        ThrowForAzure("audio");
        ValidateTranscriptionRequest(input);

        var request = new OpenAIRequest("/audio/transcriptions", Method.Post);
        var fileStream = await FileManagementClient.DownloadAsync(input.File);
        var fileBytes = await fileStream.GetByteData();
        request.AddFile("file", fileBytes, input.File.Name);
        request.AddParameter("model", input.Model);
        request.AddParameter("response_format", GetResponseFormat(input.Model));
        request.AddParameter("temperature", input.Temperature ?? 0);
        request.AddParameter("language", input.Language);
        request.AddParameter("prompt", input.Prompt);
        
        if (input.TimestampGranularities is not null && input.TimestampGranularities.Any())
        {
            foreach (var granularity in input.TimestampGranularities)
            {
                request.AddParameter("timestamp_granularities[]", granularity);
            }
        }

        if (input.KnownSpeakerNames is not null && input.KnownSpeakerNames.Any())
        {
            foreach (var knownSpeakerName in input.KnownSpeakerNames)
            {
                request.AddParameter("known_speaker_names[]", knownSpeakerName);
            }
        }      
        
        var response = await UniversalClient.ExecuteWithErrorHandling<TranscriptionDto>(request);
        var words = response.Words?.Select(x => new WordResponse(x)).ToList() ?? new List<WordResponse>();
        var segments = response.Segments?.Select(x => new SegmentResponse(x)).ToList() ?? new List<SegmentResponse>();
        
        return new()
        {
            Transcription = response.Text,
            Words = JsonConvert.SerializeObject(words),
            Segments = JsonConvert.SerializeObject(segments)
        };

        static string GetResponseFormat(string model) => model switch
        {
            "whisper-1" => "verbose_json",
            "gpt-4o-transcribe-diarize" => "diarized_json",
            _ => "json"
        };

        static void ValidateTranscriptionRequest(TranscriptionRequest input)
        {
            bool isDiarizationModel = string.Equals(input.Model, "gpt-4o-transcribe-diarize", StringComparison.OrdinalIgnoreCase);
            int speakerCount = input.KnownSpeakerNames?.Count() ?? 0;

            if (isDiarizationModel && input.Prompt is not null)
            {
                throw new PluginMisconfigurationException("Prompt parameter is not supported when using the 'gpt-4o-transcribe-diarize' model.");
            }

            switch (speakerCount)
            {
                case > 4:
                    throw new PluginMisconfigurationException("Known speaker names parameter supports a maximum of 4 names.");
                case > 0 when !isDiarizationModel:
                    throw new PluginMisconfigurationException("Known speaker names parameter is supported only for 'gpt-4o-transcribe-diarize' model.");
            }
        }
    }

    [Action("Create speech", Description = "Generates audio from the text input.")]
    public async Task<CreateSpeechResponse> CreateSpeech(
        [ActionParameter] SpeechCreationModelIdentifier modelIdentifier,
        [ActionParameter] CreateSpeechRequest input)
    {
        ThrowForAzure("audio");
        var model = modelIdentifier.ModelId ?? "tts-1-hd";
        var responseFormat = input.ResponseFormat ?? "mp3";

        var request = new OpenAIRequest("/audio/speech", Method.Post);
        request.AddJsonBody(new
        {
            model,
            input = input.InputText,
            voice = input.Voice ?? "alloy",
            response_format = responseFormat,
            speed = input.Speed ?? 1.0f
        });

        var response = await UniversalClient.ExecuteWithErrorHandling(request);

        using var stream = new MemoryStream(response.RawBytes);
        var file = await FileManagementClient.UploadAsync(stream, response.ContentType,
            $"{input.OutputAudioName ?? input.Voice}.{responseFormat}");

        return new(file);
    }
}