using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Chat;

public class TranslateXliffRequest
{
    public FileReference File { get; set; }
    
    [Display("Post edit locked segments", Description = "By default it set to false. If true, OpenAI will not change the segments that are locked in the XLIFF file.")]
    public bool? PostEditLockedSegments { get; set; }
}