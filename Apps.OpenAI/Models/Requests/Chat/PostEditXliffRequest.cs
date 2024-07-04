using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests.Chat
{
    public class PostEditXliffRequest
    {
        public FileReference File { get; set; }

        [Display("Source language")]
        public string? SourceLanguage { get; set; }

        [Display("Target language")]
        public string? TargetLanguage { get; set; }

        [Display("Update locked segments", Description = "By default it set to false. If true, OpenAI will not change the segments that are locked in the XLIFF file.")]
        public bool? PostEditLockedSegments { get; set; }
    }
}
