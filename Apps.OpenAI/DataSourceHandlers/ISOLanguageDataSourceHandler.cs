using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.DataSourceHandlers;

public class IsoLanguageDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return CultureInfo.GetCultures(CultureTypes.NeutralCultures)
            .Where(c => c.Name.Length >= 2)
            .GroupBy(c => c.Name.Substring(0, 2), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().EnglishName);
    }
}