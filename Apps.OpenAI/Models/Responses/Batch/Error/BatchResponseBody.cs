using Newtonsoft.Json;

namespace Apps.OpenAI.Models.Responses.Batch.Error;

public class BatchResponseBody
{
    [JsonProperty("error")]
    public BatchErrorDetail Error { get; set; }

    [JsonProperty("choices")]
    public object Choices { get; set; }
}