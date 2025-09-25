using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Background;

public class BackgroundDownloadRequest
{
    [Display("Batch ID"), DataSource(typeof(BatchDataSourceHandler))]
    public string BatchId { get; set; }
    
    [Display("Transformation file")]
    public FileReference TransformationFile { get; set; }

    [StaticDataSource(typeof(OpenAiProcessFileFormatHandler))]
    [Display("Output file handling", Description = "Determine the format of the output file. Default is XLIFF.")]
    public string? OutputFileHandling { get; set; }
}
