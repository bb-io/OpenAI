using Newtonsoft.Json;
using System.Collections.Generic;

namespace Apps.OpenAI.Dtos
{
    public class AssistantResponseDto
    {
        [JsonProperty("content")]
        public List<Content> Content { get; set; }
    }

    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public Text Text { get; set; }
    }

    public class Text
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
