using Blackbird.Applications.Sdk.Common;
using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests
{
    public class ChatRequest
    {
        public string Message { get; set; }

        [Display("Maximum tokens")]
        public int? MaximumTokens { get; set; }
        
        [DataSource(typeof(ChatCompletionsModelDataSourceHandler))]
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
