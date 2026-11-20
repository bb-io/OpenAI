#nullable enable

using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Content;

public class ShortenTextRequest
{
    [Display("Text")]
    public string Text { get; set; } = string.Empty;

    [Display("Maximum length", Description = "Maximum number of characters of the shortened text.")]
    public int MaximumLength { get; set; }

    [Display("Retries", Description = "Number of extra attempts when the text is still too long. Defaults to 3.")]
    public int? Retries { get; set; }

    [Display("Additional instructions", Description = "Specify additional instructions to apply when shortening.")]
    public string? AdditionalInstructions { get; set; }

    [Display("Glossary")]
    public FileReference? Glossary { get; set; }
}
