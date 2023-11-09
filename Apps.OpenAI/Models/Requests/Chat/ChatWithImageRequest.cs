using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.OpenAI.Models.Requests.Chat;

public class ChatWithImageRequest : ChatRequest
{
    public File Image { get; set; }
}