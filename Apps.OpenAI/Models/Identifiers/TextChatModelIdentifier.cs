using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class TextChatModelIdentifier
{
    [Display("Model ID")]
    [DataSource(typeof(TextChatModelDataSourceHandler))]
    public string? ModelId { get; set; }
}