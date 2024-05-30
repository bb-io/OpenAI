using System.Collections;
using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Audio;

public class TranscriptionResponse
{
    public string Transcription { get; set; }

    public List<string> Words { get; set; }
    
    public List<string> Segments { get; set; }
}

public class WordResponse
{
    public string Word { get; set; }
    
    public double Start { get; set; }
    
    public double End { get; set; }
}

public class SegmentResponse
{
    [Display("Segment ID")]
    public int Id { get; set; }
    
    public string Text { get; set; }
    
    public double Start { get; set; }
    
    public double End { get; set; }
}

