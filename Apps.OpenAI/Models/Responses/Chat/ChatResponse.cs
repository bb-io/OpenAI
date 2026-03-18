using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using System.Collections.Generic;

namespace Apps.OpenAI.Models.Responses.Chat;

public class ChatResponse
{
    [Display("System prompt")]
    public string SystemPrompt { get; set; }

    [Display("User prompt")]
    public string UserPrompt { get; set; }

    [Display("Response")]
    public string Message { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; }

    [Display("Citations")]
    public IEnumerable<UrlCitationDto> Citations { get; set; } = [];

    [Display("Sources")]
    public IEnumerable<string> Sources { get; set; } = [];
}