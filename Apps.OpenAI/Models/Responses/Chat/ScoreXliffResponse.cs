using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Responses.Chat
{
    public class ScoreXliffResponse
    {
        public FileReference File { get; set; }

        [Display("Average Score")]
        public float AverageScore { get; set; }

        [Display("Usage")]
        public UsageDto Usage { get; set; }
    }
}
