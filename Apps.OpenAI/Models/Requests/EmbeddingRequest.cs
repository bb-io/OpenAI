using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests;

public class EmbeddingRequest
{
    [Display("Text to embed")]
    public string Text { get; set; }

    [DataSource(typeof(EmbedModelDataSourceHandler))]
    public string? Model { get; set; }
}