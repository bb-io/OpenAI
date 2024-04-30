using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests;

public class CreateSpeechRequest
{
    [Display("Input text")]
    public string InputText { get; set; }

    [StaticDataSource(typeof(VoiceDataSourceHandler))]
    public string Voice { get; set; }
    
    [Display("Output audio name")]
    public string? OutputAudioName { get; set; }
    
    [Display("Response format")]
    [StaticDataSource(typeof(AudioResponseFormatDataSourceHandler))]
    public string? ResponseFormat { get; set; }
    
    [StaticDataSource(typeof(SpeedDataSourceHandler))]
    public float? Speed { get; set; }
}