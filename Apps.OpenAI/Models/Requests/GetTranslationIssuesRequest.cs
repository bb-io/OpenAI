using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Requests;

public class GetTranslationIssuesRequest
{
    [Display("Source text")]
    public string SourceText { get; set; }

    [Display("Target text")]
    public string TargetText { get; set; }
    public string? Model { get; set; }  
}