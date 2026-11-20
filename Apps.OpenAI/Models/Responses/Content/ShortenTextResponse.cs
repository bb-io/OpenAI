using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Content;

public class ShortenTextResponse
{
    [Display("Shortened text")]
    public string ShortenedText { get; set; } = string.Empty;

    [Display("System prompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [Display("Usage")]
    public UsageDto Usage { get; set; } = new();
}
