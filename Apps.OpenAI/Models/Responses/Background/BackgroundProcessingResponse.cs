using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Responses.Background;

public class BackgroundProcessingResponse
{
    [Display("Batch ID")]
    public string BatchId { get; set; }
    
    [Display("Status")]
    public string Status { get; set; }
    
    [Display("Created at")]
    public string CreatedAt { get; set; }
    
    [Display("Expected completion")]
    public string? ExpectedCompletionTime { get; set; }
    
    [Display("Transformation file")]
    public FileReference TransformationFile { get; set; }
}
