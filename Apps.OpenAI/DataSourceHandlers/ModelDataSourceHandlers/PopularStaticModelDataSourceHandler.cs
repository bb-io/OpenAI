using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Collections.Generic;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
public class PopularStaticModelDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return
        [
            new("gpt-5", "gpt-5"),
            new("gpt-5-nano", "gpt-5-nano"),
            new("gpt-4.1", "gpt-4.1"),
            new("gpt-4.1-nano", "gpt-4.1-nano"),
            new("gpt-4.1-mini", "gpt-4.1-mini"),
            new("o3", "o3"),
            new("o4-mini", "o4-mini")
        ];
    }
}