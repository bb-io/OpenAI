using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.OpenAI.Models.Content;
public class ContentProcessingResult : ITranslateFileOutput
{
    public FileReference File { get; set; }
    public UsageDto Usage { get; set; } = new UsageDto();

    [Display("Total segments")]
    public int TotalSegmentsCount { get; set; }

    [Display("Translatable segments")]
    public int TotalTranslatable { get; set; }

    [Display("Targets updated")]
    public int TargetsUpdatedCount { get; set; }

    [Display("Processed batches")]
    public int ProcessedBatchesCount { get; set; }

    [Display("System prompt")]
    public string SystemPrompt { get; set; }
}
