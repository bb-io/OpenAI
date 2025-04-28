using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Identifiers;

public class TextChatModelIdentifier
{
    [Display("Model")]
    [StaticDataSource(typeof(PopularStaticModelDataSourceHandler))]
    public string ModelId { get; set; }

    [Display("Advanced model", Description = "Browse more models than the static list under 'Model'. This value will replace 'Model'.")]
    [DataSource(typeof(TextChatModelDataSourceHandler))]
    public string? AdvancedModelId { get; set; }

    public string GetModel()
    {
        return AdvancedModelId == null ? ModelId : AdvancedModelId;
    }
}