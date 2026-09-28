using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class ImageEditingModelIdentifier
{
    [Display("Model ID")]
    [DataSource(typeof(ImageEditingModelDataSourceHandler))]
    public string ModelId { get; set; }
}
