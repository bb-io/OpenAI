using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests;

public class GetTranslationIssuesRequest
{
    [Display("Source text")]
    public string SourceText { get; set; }

    [Display("Target text")]
    public string TargetText { get; set; }
    
    [DataSource(typeof(ChatCompletionsModelDataSourceHandler))]
    public string? Model { get; set; }  
}