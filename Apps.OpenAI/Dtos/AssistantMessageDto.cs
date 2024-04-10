using Newtonsoft.Json;
using System.Collections.Generic;

namespace Apps.OpenAI.Dtos
{
    public class AssistantMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }

        [JsonProperty("file_ids")]
        public IEnumerable<string> FileIds { get; set;}
    }
}
