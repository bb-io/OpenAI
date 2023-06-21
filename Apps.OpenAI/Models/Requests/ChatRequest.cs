using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Model.Requests
{
    public class ChatRequest
    {
        public string Message { get; set; }

        [Display("Maximum tokens")]
        public int MaximumTokens { get; set; }
        public string Model { get; set; }
    }
}
