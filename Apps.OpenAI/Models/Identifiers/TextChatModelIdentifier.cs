using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

namespace Apps.OpenAI.Models.Identifiers;

public class TextChatModelIdentifier
{
    [Display("Model", Description =
        "Required for standard OpenAI connection type. " +
        "Ovewrites the model from the connection for OpenAI (embedded). " +
        "Does not work for Azure OpenAI connection type (specify the deployment name during connection)")] 
    [DataSource(typeof(TextChatModelDataSourceHandler))]
    public string? ModelId { get; set; }
}