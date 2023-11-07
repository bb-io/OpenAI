using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests;

public class CreateSpeechRequest
{
    [Display("Input text")]
    public string InputText { get; set; }
    
    [Display("Output audio name")]
    public string OutputAudioName { get; set; }
    
    [DataSource(typeof(VoiceDataSourceHandler))]
    public string Voice { get; set; }
    
    [Display("Response format")]
    [DataSource(typeof(AudioResponseFormatDataSourceHandler))]
    public string? ResponseFormat { get; set; }
    
    [DataSource(typeof(SpeedDataSourceHandler))]
    public float? Speed { get; set; }
}