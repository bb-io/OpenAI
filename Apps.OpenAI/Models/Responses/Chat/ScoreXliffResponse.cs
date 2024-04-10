using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Responses.Chat
{
    public class ScoreXliffResponse
    {
        public FileReference File { get; set; }

        [Display("Average Score")]
        public float AverageScore { get; set; }
    }
}
