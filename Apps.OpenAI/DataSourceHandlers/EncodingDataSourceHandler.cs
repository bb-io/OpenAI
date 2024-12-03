using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.DataSourceHandlers;

public class EncodingDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>()
        {
            new( "cl100k_base", "cl100k base" ),
            new( "p50k_base", "p50k base"),
        };
    }
}