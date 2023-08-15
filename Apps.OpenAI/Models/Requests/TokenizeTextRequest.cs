using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests;

public class TokenizeTextRequest
{
    public string Text { get; set; }
    
    [DataSource(typeof(EncodingDataSourceHandler))]
    public string? Encoding { get; set; }
}