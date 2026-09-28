using System.Collections.Generic;
using Newtonsoft.Json;

namespace Apps.OpenAI.Dtos;

public class ImageLocalizationExtractionDto
{
    [JsonProperty("source_language")]
    public string SourceLanguage { get; set; }

    [JsonProperty("segments")]
    public List<ImageLocalizationSegmentDto> Segments { get; set; } = [];
}

public class ImageLocalizationSegmentDto
{
    [JsonProperty("text")]
    public string Text { get; set; }
}
