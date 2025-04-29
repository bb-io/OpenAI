using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;
public class PopularStaticModelDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>()
        {
            new DataSourceItem("gpt-4.1", "gpt-4.1"),
            new DataSourceItem("gpt-4.1-nano", "gpt-4.1-nano"),
            new DataSourceItem("gpt-4.1-mini", "gpt-4.1-mini"),
            new DataSourceItem("o3", "o3"),
            new DataSourceItem("o4-mini", "o4-mini")
        };
    }
}
