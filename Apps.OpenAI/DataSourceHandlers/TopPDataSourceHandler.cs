using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.DataSourceHandlers;

public class TopPDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.0f, 1.0f, 0.1f)
            .Select(x => new DataSourceItem(x, x));
    }
}