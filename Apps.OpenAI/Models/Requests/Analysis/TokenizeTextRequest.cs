using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Analysis;

public class TokenizeTextRequest
{
    public string Text { get; set; }
    
    [StaticDataSource(typeof(EncodingDataSourceHandler))]
    public string? Encoding { get; set; }
}