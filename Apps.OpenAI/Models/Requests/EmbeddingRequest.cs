using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Requests
{
    public class EmbeddingRequest
    {
        [Display("Text to embed")]
        public string Text { get; set; }

        public string? Model { get; set; }
    }
}