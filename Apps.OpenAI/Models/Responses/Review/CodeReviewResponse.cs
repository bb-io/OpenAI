using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using System.Collections.Generic;

namespace Apps.OpenAI.Models.Responses.Review;

public class CodeReviewResponse
{
    [Display("Summary")]
    public string Summary { get; set; } = string.Empty;

    [Display("Findings")]
    public IEnumerable<CodeReviewFinding> Findings { get; set; } = [];

    [Display("System prompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [Display("User prompt")]
    public string UserPrompt { get; set; } = string.Empty;

    [Display("Usage")]
    public UsageDto Usage { get; set; } = new();
}

public class CodeReviewFinding
{
    [Display("Path")]
    public string Path { get; set; } = string.Empty;

    [Display("Line")]
    public int Line { get; set; }

    [Display("Side")]
    public string Side { get; set; } = "RIGHT";

    [Display("Severity")]
    public string Severity { get; set; } = string.Empty;

    [Display("Category")]
    public string Category { get; set; } = string.Empty;

    [Display("Title")]
    public string Title { get; set; } = string.Empty;

    [Display("Body")]
    public string Body { get; set; } = string.Empty;

    [Display("Suggestion")]
    public string Suggestion { get; set; } = string.Empty;
}
