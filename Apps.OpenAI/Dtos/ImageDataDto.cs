using Newtonsoft.Json;

namespace Apps.OpenAI.Dtos;

public record ImageDataDto
{
    [JsonProperty("b64_json")]
    public string Base64 { get; set; }
}