using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.Models.Requests.Chat;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Image;

public class ExtractLocalizableContentFromImageRequest : BaseChatRequest
{
    public FileReference Image { get; set; }

    [Display("Source language", Description = "Optional source language. If omitted, the model detects it from the image.")]
    [StaticDataSource(typeof(IsoLanguageDataSourceHandler))]
    public string? SourceLanguage { get; set; }

    [Display("Output file format")]
    [StaticDataSource(typeof(ImageLocalizationOutputFormatDataSourceHandler))]
    public string? OutputFileFormat { get; set; }

    [Display("Output file name", Description = "The name of the output file without the extension.")]
    public string? OutputFileName { get; set; }
}
