using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Review;

public class MqmBackgroundResponse
{
    [Display("Combined report")]
    public string CombinedReport { get; set; }
    
    [Display("Segment reports")]
    public List<SegmentMqmReport> SegmentReports { get; set; }
    
    public UsageDto Usage { get; set; }
    
    [Display("Processed segments")]
    public int ProcessedSegments { get; set; }
    
    [Display("Total segments")]
    public int TotalSegments { get; set; }
    
    [Display("Batch status")]
    public string BatchStatus { get; set; }
}

public class SegmentMqmReport
{
    [Display("Source text")]
    public string SourceText { get; set; }
    
    [Display("Target text")]
    public string TargetText { get; set; }
    
    [Display("MQM report")]
    public string MqmReport { get; set; }
}
