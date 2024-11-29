using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Chat;

public class LocalizeTextRequest : BaseChatRequest
{
    public string Text { get; set; }
        
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string Locale { get; set; }
}