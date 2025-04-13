using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using System.Collections.Generic;

namespace Apps.OpenAI.Models.Requests.Chat
{
    public class ScoreXliffRequest
    {
        public FileReference File { get; set; }

        [Display("Source Language")]
        public string? SourceLanguage { get; set; }

        [Display("Target Language")]
        public string? TargetLanguage { get; set; }

        public IEnumerable<double>? Threshold { get; set; }

        [StaticDataSource(typeof(ConditionDataSourceHandler))]
        public IEnumerable<string>? Condition { get; set; }

        [Display("New target state")]
        [StaticDataSource(typeof(XliffStateDataSourceHandler))]
        public IEnumerable<string>? State { get; set; }
    }
}
