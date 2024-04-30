using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.Models.Requests.Chat
{
    public class RepurposeRequest : BaseChatRequest
    {
        [Display("Target audience")]
        public string? TargetAudience { get; set; }

        [Display("Tone of voice")]
        [StaticDataSource(typeof(ToneOfVoiceHandler))]
        public string? ToneOfVOice { get; set; }

        [Display("Locale")]
        [StaticDataSource(typeof(LocaleDataSourceHandler))]
        public string? Locale { get; set; }

        [Display("Additional prompt")]
        public string? AdditionalPrompt { get; set; }
    }
}
