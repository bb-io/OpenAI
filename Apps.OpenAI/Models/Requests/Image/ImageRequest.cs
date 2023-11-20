using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Image;

public class ImageRequest
{
    public string Prompt { get; set; }
    
    [Display("Output image name")]
    public string? OutputImageName { get; set; }
        
    [DataSource(typeof(ImageSizeDataSourceHandler))]
    public string? Size { get; set; }

    [Display("Quality (only for dall-e-3)")]
    [DataSource(typeof(QualityDataSourceHandler))]
    public string? Quality { get; set; }
    
    [Display("Style (only for dall-e-3)")]
    [DataSource(typeof(StyleDataSourceHandler))]
    public string? Style { get; set; }
}