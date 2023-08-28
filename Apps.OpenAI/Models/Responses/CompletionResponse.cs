using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses;

public class CompletionResponse
{
    [Display("Completed text")]
    public string CompletionText { get; set; }
}