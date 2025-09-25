using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Background;

public class BackgroundDownloadRequest
{
    [Display("Batch ID")]
    public string BatchId { get; set; }
    
    [Display("Transformation file")]
    public FileReference TransformationFile { get; set; }
    
    [Display("Output file handling", Description = "Determine the format of the output file. Default is XLIFF.")]
    public string? OutputFileHandling { get; set; }
}
