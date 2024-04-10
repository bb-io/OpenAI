using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Requests.Analysis;

public class EmbeddingRequest
{
    [Display("Text to embed")]
    public string Text { get; set; }
}