using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.Models.Requests.Chat;

public class ReasoningEffortRequest
{
    [Display("Reasoning effort")]
    [StaticDataSource(typeof(ReasoningEffortDataSourceHandler))]
    public string? ReasoningEffort { get; set; } = "medium";
}