using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Chat;

public class EditResponse
{
    [Display("Edited text")]
    public string EditText { get; set; }
}