namespace Apps.OpenAI.Models.Requests.Chat;

public class CompletionRequest : BaseChatRequest
{
    public string Prompt { get; set; }
}