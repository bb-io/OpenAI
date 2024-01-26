using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class CompletionModelIdentifier
{
    [Display("Model ID")]
    [DataSource(typeof(CompletionModelDataSourceHandler))]
    public string? ModelId { get; set; }
}