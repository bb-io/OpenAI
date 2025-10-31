using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.Models.Identifiers;

public class ImageChatModelIdentifier
{
    [Display("Model ID")]
    [StaticDataSource(typeof(ImageChatModelDataSourceHandler))]
    public string ModelId { get; set; }
}
