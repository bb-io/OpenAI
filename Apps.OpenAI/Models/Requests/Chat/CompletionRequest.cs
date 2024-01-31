namespace Apps.OpenAI.Models.Requests.Chat;

public class CompletionRequest : BaseChatRequest
{
    public string Text { get; set; }
}