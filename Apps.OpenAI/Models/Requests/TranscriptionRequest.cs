using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests;

public class TranscriptionRequest
{
    [Display("File name")]
    public string? FileName { get; set; }
        
    public File File { get; set; }

    [Display("Language (ISO 639-1)")]
    [DataSource(typeof(ISOLanguageDataSourceHandler))]
    public string? Language { get; set; }

    [Display("Temperature")]
    [DataSource(typeof(TemperatureDataSourceHandler))]
    public float? Temperature { get; set; }
}