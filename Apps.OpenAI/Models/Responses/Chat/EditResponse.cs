using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Chat;

public class EditResponse
{
    [Display("System prompt")]
    public string SystemPrompt { get; set; }

    [Display("User prompt")]
    public string UserPrompt { get; set; }

    [Display("Response")]
    public string EditText { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; }
}