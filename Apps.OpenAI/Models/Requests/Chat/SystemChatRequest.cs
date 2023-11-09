using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Requests.Chat;

public class SystemChatRequest : BaseChatRequest
{
    [Display("System prompt")]
    public string SystemPrompt { get; set; }
        
    public string Message { get; set; }
}