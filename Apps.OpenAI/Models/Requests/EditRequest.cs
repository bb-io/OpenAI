using Blackbird.Applications.Sdk.Common;
using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests;

public class EditRequest
{
    [Display("Input text")]
    public string InputText { get; set; }

    public string Instruction { get; set; }
        
    [DataSource(typeof(EditsModelDataSourceHandler))]
    public string? Model { get; set; }

    [Display("Temperature")]
    [DataSource(typeof(TemperatureDataSourceHandler))]
    public float? Temperature { get; set; }

    [Display("top_p")]
    [DataSource(typeof(TopPDataSourceHandler))]
    public float? TopP { get; set; }
}