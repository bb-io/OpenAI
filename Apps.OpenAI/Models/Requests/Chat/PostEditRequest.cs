using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Chat;

public class PostEditRequest
{
    [Display("Source text")]
    public string SourceText { get; set; }

    [Display("Target text")]
    public string TargetText { get; set; }

    [Display("Additional prompt")]
    public string? AdditionalPrompt { get; set; }
}