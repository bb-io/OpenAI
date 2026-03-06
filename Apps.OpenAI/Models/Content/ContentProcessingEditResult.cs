using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Content;

public class ContentProcessingEditResult
{
    public FileReference File { get; set; }
    public UsageDto Usage { get; set; } = new UsageDto();
    
    [Display("Total segments")]
    public int TotalSegmentsCount { get; set; }
    
    [Display("Total segments reviewed")]
    public int TotalSegmentsReviewed { get; set; }
    
    [Display("Total segments updated")]
    public int TotalSegmentsUpdated { get; set; }
    
    [Display("Total segments with errors")]
    public int TotalSegmentsWithErrors { get; set; }
    
    [Display("Processed batches")]
    public int ProcessedBatchesCount { get; set; }

    [Display("System prompt")]
    public string SystemPrompt { get; set; }
    
    [Display("Error details")]
    public List<string>? ErrorDetails { get; set; }
}
