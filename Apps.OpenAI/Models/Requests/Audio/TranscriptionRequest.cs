using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using System.Collections.Generic;

namespace Apps.OpenAI.Models.Requests.Audio;

public class TranscriptionRequest
{
    public FileReference File { get; set; }

    [Display("Model")]
    [StaticDataSource(typeof(AudioTranscriptionDataSourceHandler))]
    public required string Model { get; set; }

    [Display("Language (ISO 639-1)")]
    [StaticDataSource(typeof(IsoLanguageDataSourceHandler))]
    public string? Language { get; set; }

    [Display("Temperature")]
    [StaticDataSource(typeof(TemperatureDataSourceHandler))]
    public float? Temperature { get; set; }

    [Display("Timestamp granularities", Description = "By default, the API returns timestamps at the segment level.")]
    [StaticDataSource(typeof(TimestampGranularitiesSourceHandler))]
    public IEnumerable<string>? TimestampGranularities { get; set; }

    [Display("Prompt", Description = "Text to guide the model's style or continue a previous audio segment, should match the audio language. " +
                                     "Not supported when using 'gpt-4o-transcribe-diarize'.")]
    public string? Prompt { get; set; }

    [Display("Known speaker names", Description = "Optional list of speaker identifiers (short labels; maximum of 4 speakers supported). " +
                                                  "Only supported when using 'gpt-4o-transcribe-diarize'.")]
    public IEnumerable<string>? KnownSpeakerNames { get; set; }
}