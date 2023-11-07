using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class ModelIdentifier
{
    [DataSource(typeof(ModelDataSourceHandler))]
    public string? Model { get; set; }
}