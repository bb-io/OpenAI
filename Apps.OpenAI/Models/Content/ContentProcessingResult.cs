extern alias XliffContent;

using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XliffContent::Blackbird.Xliff.Utils.Models.Content;

namespace Apps.OpenAI.Models.Content;
public class ContentProcessingResult
{
    public FileReference Content { get; set; }
    public UsageDto Usage { get; set; } = new UsageDto();

    [Display("Total segments")]
    public int TotalSegmentsCount { get; set; }

    [Display("Translatable segments")]
    public int TotalTranslatable { get; set; }

    [Display("Targets updated")]
    public int TargetsUpdatedCount { get; set; }

    [Display("Processed batches")]
    public int ProcessedBatchesCount { get; set; }
}
