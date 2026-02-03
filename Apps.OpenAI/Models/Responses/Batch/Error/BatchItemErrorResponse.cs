using Newtonsoft.Json;

namespace Apps.OpenAI.Models.Responses.Batch.Error;

public class BatchItemErrorResponse
{
    [JsonProperty("response")]
    public BatchResponseInfo Response { get; set; }
}