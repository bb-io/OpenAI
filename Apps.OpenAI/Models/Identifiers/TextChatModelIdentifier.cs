using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class TextChatModelIdentifier
{
    [Display("Model ID", Description = "Default model ID: gpt-4-turbo-preview")]
    [DataSource(typeof(TextChatModelDataSourceHandler))]
    public string ModelId { get; set; }
}