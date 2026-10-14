using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.DataSourceHandlers;

public class DocumentSummaryDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return
        [
            new("auto", "Auto"),
            new("always", "Always"),
            new("never", "Never")
        ];
    }
}
