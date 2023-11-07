using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class ModelIdentifier
{
    [Display("Model")]
    [DataSource(typeof(ModelDataSourceHandler))]
    public string? ModelId { get; set; }
}