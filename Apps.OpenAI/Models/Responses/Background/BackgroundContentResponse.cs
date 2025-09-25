using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Responses.Background;

public class BackgroundContentResponse
{
    public FileReference File { get; set; }
    public UsageDto Usage { get; set; } = new UsageDto();

    [Display("Total segments")]
    public int TotalSegmentsCount { get; set; }

    [Display("Processed segments")]
    public int ProcessedSegmentsCount { get; set; }

    [Display("Updated segments")]
    public int UpdatedSegmentsCount { get; set; }

    [Display("Batch status")]
    public string BatchStatus { get; set; }
}
