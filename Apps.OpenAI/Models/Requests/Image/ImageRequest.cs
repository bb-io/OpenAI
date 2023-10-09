using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Image;

public class ImageRequest
{
    public string Prompt { get; set; }
        
    [DataSource(typeof(ImageSizeDataSourceHandler))]
    public string? Size { get; set; }
}