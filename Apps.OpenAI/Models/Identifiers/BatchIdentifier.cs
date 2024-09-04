using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class BatchIdentifier
{
    [Display("Batch ID"), DataSource(typeof(BatchDataSourceHandler))]
    public string BatchId { get; set; } = string.Empty;
}