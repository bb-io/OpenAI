using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class SpeechCreationModelIdentifier
{
    [Display("Model ID")]
    [DataSource(typeof(SpeechCreationModelDataSourceHandler))]
    public string ModelId { get; set; }
}