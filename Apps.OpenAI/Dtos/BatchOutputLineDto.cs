#nullable enable

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.OpenAI.Dtos;

// One line of a batch output or error file.
public class BatchOutputLineDto
{
    [JsonProperty("custom_id")]
    public string CustomId { get; set; } = string.Empty;

    [JsonProperty("response")]
    public BatchOutputResponseDto? Response { get; set; }

    [JsonProperty("error")]
    public BatchOutputErrorDto? Error { get; set; }
}

public class BatchOutputResponseDto
{
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }

    [JsonProperty("body")]
    public JObject? Body { get; set; }
}

public class BatchOutputErrorDto
{
    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }
}
