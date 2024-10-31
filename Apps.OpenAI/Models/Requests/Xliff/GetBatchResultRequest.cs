using Apps.OpenAI.Models.Identifiers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Xliff;

public class GetBatchResultRequest : BatchIdentifier
{
    [Display("Original XLIFF")]
    public FileReference OriginalXliff { get; set; }

    [Display("Add missing leading/trailing tags", Description = "If true, missing leading tags will be added to the target segment.")]
    public bool? AddMissingTrailingTags { get; set; }
}