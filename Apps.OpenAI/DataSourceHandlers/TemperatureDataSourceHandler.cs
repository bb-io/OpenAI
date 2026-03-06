using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Collections.Generic;
using System.Globalization;

namespace Apps.OpenAI.DataSourceHandlers;

public class TemperatureDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new DataSourceItem[]
        {
            new DataSourceItem(0.2f.ToString("0.00", CultureInfo.InvariantCulture), "0.2 | Governed"),
            new DataSourceItem(0.6f.ToString("0.00", CultureInfo.InvariantCulture), "0.6 | Balanced"),
            new DataSourceItem(1.0f.ToString("0.00", CultureInfo.InvariantCulture), "1.0 | Expressive"),
            new DataSourceItem(1.2f.ToString("0.00", CultureInfo.InvariantCulture), "1.2 | Exploratory"),
            new DataSourceItem(1.6f.ToString("0.00", CultureInfo.InvariantCulture), "1.6 | Experimental"),
        };
    }
}