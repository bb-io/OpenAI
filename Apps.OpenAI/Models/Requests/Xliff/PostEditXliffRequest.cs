using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests.Xliff
{
    public class PostEditXliffRequest
    {
        public FileReference File { get; set; }

        [Display("Source language")]
        public string SourceLanguage { get; set; }

        [Display("Target language")]
        public string TargetLanguage { get; set; }

        [Display("Filter glossary terms")]
        public bool? FilterGlossary { get; set; }

        [Display("Update locked segments", Description = "By default it set to false. If true, OpenAI will not change the segments that are locked in the XLIFF file.")]
        public bool? PostEditLockedSegments { get; set; }
        
        [Display("Add missing trailing tags", Description = "If true, missing trailing tags will be added to the target segment.")]
        public bool? AddMissingTrailingTags { get; set; }
    }
}
