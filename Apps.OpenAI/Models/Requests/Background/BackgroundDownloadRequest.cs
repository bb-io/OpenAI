using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Background;

public class BackgroundDownloadRequest
{
    [Display("Batch ID", Description = "The batch the transformation file was started with. Optional for files from 'Translate in background' and 'Edit in background', which carry their batch ID."), DataSource(typeof(BatchDataSourceHandler))]
    public string? BatchId { get; set; }
    
    [Display("Transformation file")]
    public FileReference TransformationFile { get; set; }

    [StaticDataSource(typeof(OpenAiProcessFileFormatHandler))]
    [Display("Output file handling", Description = "Determine the format of the output file. Default is the format chosen when the background job was started, otherwise XLIFF.")]
    public string? OutputFileHandling { get; set; }
}
