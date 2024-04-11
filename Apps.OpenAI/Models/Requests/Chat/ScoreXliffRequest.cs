using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Chat
{
    public class ScoreXliffRequest
    {
        public FileReference File { get; set; }

        [Display("Source Language")]
        public string? SourceLanguage { get; set; }

        [Display("Target Language")]
        public string? TargetLanguage { get; set; }

        public float? Threshold { get; set; }

        [StaticDataSource(typeof(ConditionDataSourceHandler))]
        public string? Condition { get; set; }

        [Display("New Target State")]
        [DataSource(typeof(XliffStateDataSourceHandler))]
        public string? State { get; set; }
    }
}
