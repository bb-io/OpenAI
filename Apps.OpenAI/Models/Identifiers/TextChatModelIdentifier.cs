using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

namespace Apps.OpenAI.Models.Identifiers;

public class TextChatModelIdentifier
{
    [Display("Model"), DataSource(typeof(TextChatModelDataSourceHandler))]
    public string? ModelId { get; set; }
}