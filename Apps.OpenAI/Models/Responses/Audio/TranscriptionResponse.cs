using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Audio;

public class TranscriptionResponse
{
    public string Transcription { get; set; }

    [Display("Words (serialized)")]
    public string Words { get; set; }
    
    [Display("Segments (serialized)")]
    public string Segments { get; set; }
}

public record WordResponse(WordDto dto)
{
    public string Word { get; set; } = dto.Word;

    public double Start { get; set; } = dto.Start;

    public double End { get; set; } = dto.End;
}

public record SegmentResponse(SegmentDto dto)
{
    [Display("Segment ID")]
    public string Id { get; set; } = dto.Id;

    public string Text { get; set; } = dto.Text;

    public double Start { get; set; } = dto.Start;

    public double End { get; set; } = dto.End;
}

