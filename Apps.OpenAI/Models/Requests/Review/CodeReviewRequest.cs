using Apps.OpenAI.Models.Requests.Chat;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Requests.Review;

public class CodeReviewRequest : BaseChatRequest
{
    [Display("Pull request title")]
    public string? PullRequestTitle { get; set; }

    [Display("Pull request description")]
    public string? PullRequestDescription { get; set; }

    [Display("Code changes JSON",
        Description = "Structured JSON containing changed files and context. Include path, patch, and optionally file content or other review context.")]
    public string CodeChangesJson { get; set; } = string.Empty;

    [Display("Previous findings JSON",
        Description = "Optional JSON with previous review findings. Useful for incremental follow-up reviews to avoid repeating old comments.")]
    public string? PreviousFindingsJson { get; set; }

    [Display("Additional instructions",
        Description = "Optional review guidance, repository rules, or areas to focus on.")]
    public string? AdditionalInstructions { get; set; }
}
