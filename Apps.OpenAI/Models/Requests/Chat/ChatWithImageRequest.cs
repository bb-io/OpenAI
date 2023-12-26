using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Chat;

public class ChatWithImageRequest : ChatRequest
{
    public FileReference Image { get; set; }
}