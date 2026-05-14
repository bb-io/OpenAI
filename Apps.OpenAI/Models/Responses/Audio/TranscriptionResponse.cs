using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Audio;

public class TranscriptionResponse
{
    public string Transcription { get; set; }

    [Display("Words")]
    public List<WordResponse> Words { get; set; }
    
    [Display("Segments")]
    public List<SegmentResponse> Segments { get; set; }
}

public class WordResponse(WordDto dto)
{
    public string Word { get; set; } = dto.Word;

    public double Start { get; set; } = dto.Start;

    public double End { get; set; } = dto.End;
}

public class SegmentResponse(SegmentDto dto)
{
    [Display("Segment ID")]
    public string Id { get; set; } = dto.Id;

    public string? Type { get; set; } = dto.Type;

    public string Text { get; set; } = dto.Text;

    public string? Speaker { get; set; } = dto.Speaker;

    public double Start { get; set; } = dto.Start;

    public double End { get; set; } = dto.End;
}

