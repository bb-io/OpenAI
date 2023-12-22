namespace Apps.OpenAI.Models.Requests.Chat;

public class ExecuteBlackbirdPromptRequest : BaseChatRequest
{
    public string Prompt { get; set; }
}