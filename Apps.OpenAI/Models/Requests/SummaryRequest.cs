using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests
{
    public class SummaryRequest
    {
        public string Text { get; set; }
        public string Model { get; set; }

        [Display("Maximum tokens")]
        public int MaximumTokens { get; set; }
    }
}
