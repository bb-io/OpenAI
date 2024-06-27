using Newtonsoft.Json;
using System.Collections.Generic;

namespace Apps.OpenAI.Dtos
{
    public class RunDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("created_at")]
        public int? CreatedAt { get; set; }

        [JsonProperty("assistant_id")]
        public string AssistantId { get; set; }

        [JsonProperty("thread_id")]
        public string ThreadId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("started_at")]
        public int? StartedAt { get; set; }

        [JsonProperty("expires_at")]
        public int? ExpiresAt { get; set; }

        [JsonProperty("cancelled_at")]
        public int? CancelledAt { get; set; }

        [JsonProperty("failed_at")]
        public int? FailedAt { get; set; }

        [JsonProperty("completed_at")]
        public int? CompletedAt { get; set; }

        [JsonProperty("last_error")]
        public object LastError { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("instructions")]
        public string Instructions { get; set; }

        [JsonProperty("file_ids")]
        public List<string> FileIds { get; set; }

        [JsonProperty("usage")]
        public UsageDto Usage { get; set; }
    }
}
