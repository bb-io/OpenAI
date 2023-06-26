using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests
{
    public class PostEditRequest
    {
        [Display("Source text")]
        public string SourceText { get; set; }

        [Display("Target text")]
        public string TargetText { get; set; }
        public string Model { get; set; }        
    }
}
