using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Chat;

public class GetLocalizableContentFromImageRequest : BaseChatRequest
{
    public FileReference Image { get; set; }
}