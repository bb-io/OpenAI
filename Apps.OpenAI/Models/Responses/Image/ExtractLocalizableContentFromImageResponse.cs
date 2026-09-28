using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Responses.Image;

public class ExtractLocalizableContentFromImageResponse
{
    [Display("File")]
    public FileReference File { get; set; }

    [Display("Source language")]
    public string SourceLanguage { get; set; }

    [Display("Extracted segments count")]
    public int SegmentsCount { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; }
}
