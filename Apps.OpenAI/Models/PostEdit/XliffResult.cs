using System.Collections.Generic;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.PostEdit;

public class XliffResult
{
    public FileReference File { get; set; }
    public UsageDto Usage { get; set; }
    public int TargetsUpdatedCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public int ProcessedBatchesCount { get; set; }
    public int TotalSegmentsCount { get; set; }
    public int LockedSegmentsExcludeCount { get; set; }
}
