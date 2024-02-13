using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.Models.Requests.Chat
{
    public class RepurposeRequest : BaseChatRequest
    {
        [Display("Target audience")]
        public string? TargetAudience { get; set; }

        [Display("Tone of voice")]
        [DataSource(typeof(ToneOfVoiceHandler))]
        public string? ToneOfVOice { get; set; }

        [Display("Locale")]
        [DataSource(typeof(LocaleDataSourceHandler))]
        public string? Locale { get; set; }

        [Display("Additional prompt")]
        public string? AdditionalPrompt { get; set; }
    }
}
