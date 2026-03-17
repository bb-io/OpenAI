#nullable enable

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Apps.OpenAI.Dtos;

public class OpenAiResponseDto
{
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("incomplete_details")]
    public ResponseIncompleteDetailsDto? IncompleteDetails { get; set; }

    [JsonProperty("output")]
    public List<ResponseOutputItemDto> Output { get; set; } = [];

    [JsonProperty("usage")]
    public ResponseUsageDto? Usage { get; set; }
}

public class ResponseIncompleteDetailsDto
{
    [JsonProperty("reason")]
    public string? Reason { get; set; }
}

public class ResponseUsageDto
{
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }

    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}

public class ResponseOutputItemDto
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("content")]
    public List<ResponseOutputContentDto>? Content { get; set; }

    [JsonProperty("action")]
    public WebSearchActionDto? Action { get; set; }
}

public class ResponseOutputContentDto
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("annotations")]
    public List<ResponseAnnotationDto>? Annotations { get; set; }
}

public class ResponseAnnotationDto
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }
}

public class WebSearchActionDto
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("sources")]
    public List<WebSearchSourceDto>? Sources { get; set; }
}

public class WebSearchSourceDto
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}

#nullable disable
