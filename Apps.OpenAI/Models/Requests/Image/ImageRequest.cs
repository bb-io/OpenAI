using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Image;

public class ImageRequest
{
    public string Prompt { get; set; }
    
    [Display("Output image name", Description = "The name of the output image without the extension.")]
    public string? OutputImageName { get; set; }
        
    [StaticDataSource(typeof(ImageSizeDataSourceHandler))]
    public string? Size { get; set; }

    [Display("Quality (only for dall-e-3)")]
    [StaticDataSource(typeof(QualityDataSourceHandler))]
    public string? Quality { get; set; }
    
    [Display("Style (only for dall-e-3)")]
    [StaticDataSource(typeof(StyleDataSourceHandler))]
    public string? Style { get; set; }
}