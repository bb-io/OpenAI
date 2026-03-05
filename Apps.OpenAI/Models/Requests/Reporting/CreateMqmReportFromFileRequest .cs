using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.Models.Identifiers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Reporting
{
    public class CreateMqmReportFromFileRequest : TextChatModelIdentifier
    {
        [Display("File")]
        public FileReference File { get; set; }

        [Display("Source language"), StaticDataSource(typeof(LocaleDataSourceHandler))]
        public string? SourceLanguage { get; set; }

        [Display("Target language"), StaticDataSource(typeof(LocaleDataSourceHandler))]
        public string? TargetLanguage { get; set; }

        [Display("Target audience", Description = "Specify the target audience for the translation")]
        public string? TargetAudience { get; set; }

        [Display("Include finalized segments",
            Description = "By default finalized/locked segments are excluded. Enable to include them.")]
        public bool? IncludeFinalizedSegments { get; set; }

        [Display("Additional prompt instructions", Description = "Appended to the MQM system prompt.")]
        public string? AdditionalPrompt { get; set; }

        [Display("System prompt (fully replaces MQM instructions)",
            Description = "If provided, it replaces default MQM system prompt entirely.")]
        public string? CustomSystemPrompt { get; set; }

        [Display("Maximum tokens")]
        public int? MaximumTokens { get; set; }

        public FileReference? Glossary { get; set; }
    }
}
