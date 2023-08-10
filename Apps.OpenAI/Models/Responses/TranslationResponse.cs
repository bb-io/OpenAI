using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses;

public class TranslationResponse
{
    [Display("Translated text")]
    public string TranslatedText { get; set; }
}