using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.DataSourceHandlers;

public class IsoLanguageDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return CultureInfo.GetCultures(CultureTypes.NeutralCultures)
            .Where(c => c.Name.Length >= 2)
            .GroupBy(c => c.Name.Substring(0, 2), StringComparer.OrdinalIgnoreCase)
            .Select(g => new DataSourceItem(g.Key, g.First().EnglishName));
    }
}