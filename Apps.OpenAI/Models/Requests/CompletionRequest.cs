using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests
{
    public class CompletionRequest
    {
        public string Prompt { get; set; }

        [Display("Maximum tokens")]
        public int? MaximumTokens { get; set; }
        
        [DataSource(typeof(CompletionsModelDataSourceHandler))]
        public string? Model { get; set; }

        [Display("Temperature")]
        public float? Temperature { get; set; }

        [Display("top_p")]
        public float? TopP { get; set; }

        [Display("Presence penalty")]
        public float? PresencePenalty { get; set; }

        [Display("Frequency penalty")]
        public float? FrequencyPenalty { get; set; }
    }
}
