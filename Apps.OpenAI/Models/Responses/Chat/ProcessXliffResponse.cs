using System.Collections.Generic;
using Apps.OpenAI.Models.PostEdit;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Chat;

public class ProcessXliffResponse : TranslateXliffResponse
{
    [Display("Targets updated count")]
    public double TargetsUpdatedCount { get; set; }

    [Display("Total segments count")]
    public double TotalSegmentsCount { get; set; }

    [Display("Processed batches count")]
    public double ProcessedBatchesCount { get; set; }

    [Display("Error messages count")]
    public double ErrorMessagesCount { get; set; }

    [Display("Error messages")]
    public List<string> ErrorMessages { get; set; } = new();

    [Display("Locked segments exclude count")]
    public double LockedSegmentsExcludeCount { get; set; }

    public ProcessXliffResponse(XliffResult postEditResult) 
    {
        File = postEditResult.File;
        Usage = postEditResult.Usage;
        TargetsUpdatedCount = postEditResult.TargetsUpdatedCount;
        ProcessedBatchesCount = postEditResult.ProcessedBatchesCount;
        TotalSegmentsCount = postEditResult.TotalSegmentsCount;
        ErrorMessagesCount = postEditResult.ErrorMessages.Count;
        ErrorMessages = postEditResult.ErrorMessages;
        LockedSegmentsExcludeCount = postEditResult.LockedSegmentsExcludeCount;
    }
}