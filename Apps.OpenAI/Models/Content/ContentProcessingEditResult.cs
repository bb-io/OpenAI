using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Content;
public class ContentProcessingEditResult
{
    public FileReference Content { get; set; }
    public UsageDto Usage { get; set; } = new UsageDto();

    [Display("Total segments")]
    public int TotalSegmentsCount { get; set; }

    [Display("Editable segments")]
    public int TotalEditable { get; set; }

    [Display("Targets updated")]
    public int TargetsUpdatedCount { get; set; }

    [Display("Processed batches")]
    public int ProcessedBatchesCount { get; set; }
}
