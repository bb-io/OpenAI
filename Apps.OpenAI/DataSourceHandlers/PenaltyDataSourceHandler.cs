using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class PenaltyDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(-2.0f, 2.0f, 0.1f)
            .Select(p => new DataSourceItem(p, p));
    }
}