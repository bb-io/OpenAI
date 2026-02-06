using Newtonsoft.Json;

namespace Apps.OpenAI.Models.Responses.Batch.Error;

public class BatchResponseInfo
{
    [JsonProperty("body")]
    public BatchResponseBody Body { get; set; }
}
