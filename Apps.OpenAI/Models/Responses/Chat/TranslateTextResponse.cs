using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.OpenAI.Models.Responses.Chat;

public class TranslateTextResponse : ITranslateTextOutput
{
    [Display("System prompt")]
    public string SystemPrompt { get; set; }

    [Display("User prompt")]
    public string UserPrompt { get; set; }

    [Display("Translated text")]
    public string TranslatedText { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; }
}