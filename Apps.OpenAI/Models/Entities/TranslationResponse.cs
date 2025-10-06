using System.Collections.Generic;
using Newtonsoft.Json;

namespace Apps.OpenAI.Models.Entities;

public class TranslationResponse
{
    [JsonProperty("translations")]
    public List<TranslationEntity> Translations { get; set; } = new();
}
