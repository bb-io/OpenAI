using Apps.OpenAI.Models.Requests.Chat;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Xliff;
public class GetTranslationIssuesXliffRequest : BaseChatRequest
{
    [Display("XLIFF file")]
    public FileReference File { get; set; }

    [Display("Additional prompt")]
    public string? AdditionalPrompt { get; set; }

    [Display("Source langauge")]
    public string? SourceLanguage { get; set; }

    [Display("Target langauge")]
    public string? TargetLanguage { get; set; }

    [Display("Target audience")]
    public string? TargetAudience { get; set; }
}

