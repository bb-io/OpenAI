using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Responses.Chat
{
    public class GlossaryResponse
    {
        [Display("System prompt")]
        public string SystemPrompt { get; set; }

        [Display("User prompt")]
        public string UserPrompt { get; set; }
        public FileReference Glossary { get; set; }

        [Display("Usage")]
        public UsageDto Usage { get; set; }
    }
}
