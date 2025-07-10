using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.Translate;

namespace Apps.OpenAI.Models.Requests.Chat;

public class LocalizeTextRequest : BaseChatRequest, ITranslateTextInput
{
    public string Text { get; set; }

    [Display("Target language")]
    [StaticDataSource(typeof(LocaleDataSourceHandler))]
    public string TargetLanguage { get; set; }
}