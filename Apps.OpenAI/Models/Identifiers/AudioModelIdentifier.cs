using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

namespace Apps.OpenAI.Models.Identifiers;

public class AudioModelIdentifier
{
    [Display("Model"), StaticDataSource(typeof(AudioTranscriptionDataSourceHandler))]
    public string? ModelId { get; set; }
}
