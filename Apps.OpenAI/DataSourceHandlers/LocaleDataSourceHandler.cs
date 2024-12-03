using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class LocaleDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(c => new DataSourceItem(c.Name, c.DisplayName));
    }
}