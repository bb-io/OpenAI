namespace Apps.OpenAI.Dtos;

public record TranscriptionDto(string Text) : TextDto(Text)
{
    public WordDto[] Words { get; init; }
    
    public SegmentDto[] Segments { get; init; }
    
    public double Temperature { get; init; }
    
    public double AvgLogprob { get; init; }
    
    public double CompressionRatio { get; init; }
    
    public double NoSpeechProb { get; init; }
}

public record WordDto(string Word, double Start, double End);

public record SegmentDto(int Id, int Seek, double Start, double End, string Text, int[] Tokens, double Temperature, double AvgLogprob, double CompressionRatio, double NoSpeechProb);