using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
