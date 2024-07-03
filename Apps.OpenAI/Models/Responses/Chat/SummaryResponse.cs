using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Chat;

public class RepurposeResponse
{
    [Display("System prompt")]
    public string SystemPrompt { get; set; }
    public string Response { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; }
}