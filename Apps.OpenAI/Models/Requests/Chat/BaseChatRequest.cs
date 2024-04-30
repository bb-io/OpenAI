using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.Models.Requests.Chat;

public class BaseChatRequest
{
    [Display("Maximum tokens")]
    public int? MaximumTokens { get; set; }

    [Display("Temperature")]
    [StaticDataSource(typeof(TemperatureDataSourceHandler))]
    public float? Temperature { get; set; }

    [Display("top_p")]
    [StaticDataSource(typeof(TopPDataSourceHandler))]
    public float? TopP { get; set; }

    [Display("Presence penalty")]
    [StaticDataSource(typeof(PenaltyDataSourceHandler))]
    public float? PresencePenalty { get; set; }

    [Display("Frequency penalty")]
    [StaticDataSource(typeof(PenaltyDataSourceHandler))]
    public float? FrequencyPenalty { get; set; }
}