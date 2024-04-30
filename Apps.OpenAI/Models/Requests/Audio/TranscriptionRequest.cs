using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Audio;

public class TranscriptionRequest
{
    public FileReference File { get; set; }

    [Display("Language (ISO 639-1)")]
    [StaticDataSource(typeof(IsoLanguageDataSourceHandler))]
    public string? Language { get; set; }

    [Display("Temperature")]
    [StaticDataSource(typeof(TemperatureDataSourceHandler))]
    public float? Temperature { get; set; }
}