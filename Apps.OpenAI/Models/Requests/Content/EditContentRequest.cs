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
}
