using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Chat;

public class ChatRequestWithSystem : BaseChatRequest
{
    [Display("System prompt")] 
    public string SystemPrompt { get; set; }
    
    public string Message { get; set; }

    [Display("Texts",
        Description =
            "Texts that will be added to the user prompt along with the message. Useful if you want to add collection of messages to the prompt.")]
    public IEnumerable<string>? Parameters { get; set; }

    public FileReference? Image { get; set; }
}