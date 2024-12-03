using System;
using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class StyleDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>
        {
            new( "vivid", "Vivid" ),
            new( "natural", "Natural"),
        };
    }
}