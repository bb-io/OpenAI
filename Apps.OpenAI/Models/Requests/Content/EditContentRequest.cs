using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Edit;

namespace Apps.OpenAI.Models.Requests.Content;

public class EditContentRequest : IEditFileInput
{
    public FileReference File { get; set; }

    [Display("Output file handling", Description = "Determine the format of the output file. The default Blackbird behavior is to convert to XLIFF for future steps."), StaticDataSource(typeof(OpenAiProcessFileFormatHandler))]
    public string? OutputFileHandling { get; set; }

    [Display("Source language")]
    public string? SourceLanguage { get; set; }

    [Display("Target language")]
    public string? TargetLanguage { get; set; }

    [Display("Filter glossary terms")]
    public bool? FilterGlossary { get; set; }

    [Display("Modified by")]
    public string? ModifiedBy { get; set; }

    [Display("Process only segments with this state", Description = "Only segments with this state will be processed")]
    [StaticDataSource(typeof(SegmentStateDataSourceHandler))]
    public string? ProcessOnlySegmentState { get; set; }

    [Display("Max tokens", Description = "The maximum number of tokens to generate in the completion. By default it is set to 1000.")]
    public int? MaxTokens { get; set; }
}
