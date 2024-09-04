using Newtonsoft.Json;

namespace Apps.OpenAI.Dtos;

public class BatchRequestDto
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("custom_id")]
    public string CustomId { get; set; }

    [JsonProperty("response")]
    public ResponseDto Response { get; set; }

    [JsonProperty("error")]
    public object Error { get; set; }
}

public class ResponseDto
{
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }

    [JsonProperty("request_id")]
    public string RequestId { get; set; }

    [JsonProperty("body")]
    public BodyDto Body { get; set; }
}

public class BodyDto
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("object")]
    public string Object { get; set; }

    [JsonProperty("created")]
    public long Created { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("choices")]
    public ChoiceDto[] Choices { get; set; }

    [JsonProperty("usage")]
    public UsageDto Usage { get; set; }

    [JsonProperty("system_fingerprint")]
    public string SystemFingerprint { get; set; }
}

public class ChoiceDto
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("message")]
    public MessageDto Message { get; set; }

    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
}

public class MessageDto
{
    [JsonProperty("role")]
    public string Role { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }
}