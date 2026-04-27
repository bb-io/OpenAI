using Apps.OpenAI.Utils;
using Newtonsoft.Json;

namespace Apps.OpenAI.Dtos;

public record TranscriptionDto(string Text) : TextDto(Text)
{
    public WordDto[]? Words { get; init; }
    
    public SegmentDto[]? Segments { get; init; }
    
    public double Temperature { get; init; }
    
    public double AvgLogprob { get; init; }
    
    public double CompressionRatio { get; init; }
    
    public double NoSpeechProb { get; init; }
}

public record WordDto(string Word, double Start, double End);

public class SegmentDto
{
    public string? Type { get; init; }

    [JsonConverter(typeof(FlexibleIdConverter))]
    public string Id { get; init; } = string.Empty;

    public int? Seek { get; init; }

    public double Start { get; init; }

    public double End { get; init; }

    public string Text { get; init; } = string.Empty;

    public string? Speaker { get; init; }

    public int[]? Tokens { get; init; }

    public double? Temperature { get; init; }

    public double? AvgLogprob { get; init; }

    public double? CompressionRatio { get; init; }

    public double? NoSpeechProb { get; init; }
}
