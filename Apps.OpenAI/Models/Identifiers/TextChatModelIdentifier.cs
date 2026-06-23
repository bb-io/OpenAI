using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

namespace Apps.OpenAI.Models.Identifiers;

public class TextChatModelIdentifier
{
    [Display("Model", Description =
        "Optional. If empty, the app resolves the latest compatible general text model automatically for standard OpenAI connections. " +
        "Overrides the model from the connection for OpenAI (embedded). " +
        "Does not override Azure OpenAI deployment; specify the deployment name in the connection.")] 
    [DataSource(typeof(TextChatModelDataSourceHandler))]
    public string? ModelId { get; set; }
}
