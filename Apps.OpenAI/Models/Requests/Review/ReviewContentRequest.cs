using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Handlers;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Review;
using System.Collections.Generic;

namespace Apps.OpenAI.Models.Requests.Review
{
    public class ReviewContentRequest : IReviewFileInput
    {
        public FileReference File { get; set; } = new();

        [Display("Source language")]
        [StaticDataSource(typeof(LocaleDataSourceHandler))]
        public string? SourceLanguage { get; set; }

        [Display("Target language")]
        [StaticDataSource(typeof(LocaleDataSourceHandler))]
        public string TargetLanguage { get; set; } = string.Empty;

        [Display("Output file handling", Description = "Determine the format of the output file. The default Blackbird behavior is to convert to XLIFF for future steps."), StaticDataSource(typeof(ProcessFileFormatHandler))]
        public string? OutputFileHandling { get; set; }

        [Display("Score threshold", Description = "All segments above this score will automatically be finalized (0..1)")]
        public double? Threshold { get; set; }

        [Display("Model", Description = "This parameter controls which version of Claude answers your request")]
        [DataSource(typeof(TextChatModelDataSourceHandler))]
        public string Model { get; set; } = string.Empty;

        [Display("Additional instructions", Description = "Specify quality assessment criteria. For example: 'Focus on technical terminology accuracy' or 'Prioritize cultural adaptation'")]
        public string? AdditionalInstructions { get; set; }

        [Display("Max tokens", Description = "The maximum number of tokens to generate before stopping.")]
        public int? MaxTokensToSample { get; set; }

        [Display("Stop sequences", Description = "Sequences that will cause the model to stop generating completion text.")]
        public List<string>? StopSequences { get; set; }

        [Display("Temperature", Description = "Amount of randomness injected into the response.")]
        [StaticDataSource(typeof(TemperatureDataSourceHandler))]
        public string? Temperature { get; set; }

        [Display("top_p", Description = "Use nucleus sampling.")]
        [StaticDataSource(typeof(TopPDataSourceHandler))]
        public string? TopP { get; set; }

        [Display("top_k", Description = "Only sample from the top K options for each subsequent token.")]
        public int? TopK { get; set; }

        public FileReference? Glossary { get; set; }
    }
}
