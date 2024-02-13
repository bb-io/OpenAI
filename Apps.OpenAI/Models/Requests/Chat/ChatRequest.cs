using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Chat;

public class ChatRequest : BaseChatRequest
{
    [Display("System prompt")]
    public string? SystemPrompt { get; set; }
    public string Message { get; set; }
    public FileReference? Image { get; set; }
}