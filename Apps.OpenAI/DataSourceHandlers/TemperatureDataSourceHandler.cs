using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.DataSourceHandlers;

public class TemperatureDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.0f, 2.0f, 0.1f)
            .Select(t => new DataSourceItem(t, t));
    }
}