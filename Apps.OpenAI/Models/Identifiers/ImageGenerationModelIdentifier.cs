using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class ImageGenerationModelIdentifier
{
    [Display("Model ID")]
    [DataSource(typeof(ImageGenerationModelDataSourceHandler))]
    public string? ModelId { get; set; }
}