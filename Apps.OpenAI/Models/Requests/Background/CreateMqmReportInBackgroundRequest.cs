using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Requests.Background;

public class CreateMqmReportInBackgroundRequest : StartBackgroundProcessRequest
{
    [Display("Target audience", Description = "Specify the target audience for the translation")]
    public string? TargetAudience { get; set; }
}