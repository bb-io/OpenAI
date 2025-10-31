using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class EmbeddingModelIdentifier
{
    [Display("Model ID")]
    [DataSource(typeof(EmbeddingModelDataSourceHandler))]
    public string? ModelId { get; set; }
}