using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Chat;

public class LocalizeTextRequest
{
    public string Text { get; set; }
        
    [DataSource(typeof(LocaleDataSourceHandler))]
    public string Locale { get; set; }
}