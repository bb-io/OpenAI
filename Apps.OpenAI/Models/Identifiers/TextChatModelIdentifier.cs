using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.OpenAI.Models.Identifiers;

public class TextChatModelIdentifier
{
    [Display("Model"), DataSource(typeof(TextChatModelDataSourceHandler))]
    public string ModelId { get; set; } = string.Empty;

    public string GetModel()
    {
        if (string.IsNullOrEmpty(ModelId))
        {
            throw new PluginMisconfigurationException("Model ID cannot be null or empty.");
        }

        return ModelId;
    }
}