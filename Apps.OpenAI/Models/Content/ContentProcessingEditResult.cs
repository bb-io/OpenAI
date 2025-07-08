using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;

namespace Apps.OpenAI.Models.Content;
public class ContentProcessingEditResult : IEditFileOutput
{
    public FileReference File { get; set; }
    public UsageDto Usage { get; set; } = new UsageDto();

    [Display("Total segments")]
    public int TotalSegmentsCount { get; set; }

    [Display("Reviewed segments")]
    public int TotalSegmentsReviewed { get; set; }

    [Display("Edited segments")]
    public int TotalSegmentsUpdated { get; set; }

    [Display("Processed batches")]
    public int ProcessedBatchesCount { get; set; }
}
