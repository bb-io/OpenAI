using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;

namespace Apps.OpenAI.Models.Responses.Review;

public class ReviewTextResponse : IReviewTextOutput
{
    [Display("Quality score")]
    public float Score { get; set; }

    [Display("System prompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [Display("User prompt")]
    public string UserPrompt { get; set; } = string.Empty;

    public UsageDto Usage { get; set; } = new();
}