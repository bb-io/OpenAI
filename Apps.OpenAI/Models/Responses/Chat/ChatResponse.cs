using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Chat;

public class ChatResponse
{
    [Display("System prompt")]
    public string SystemPrompt { get; set; }

    [Display("User prompt")]
    public string UserPrompt { get; set; }

    [Display("Response")]
    public string Message { get; set; }
}