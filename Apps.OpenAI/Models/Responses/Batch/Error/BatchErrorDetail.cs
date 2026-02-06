using Newtonsoft.Json;

namespace Apps.OpenAI.Models.Responses.Batch.Error;

public class BatchErrorDetail
{
    [JsonProperty("message")]
    public string Message { get; set; }
}