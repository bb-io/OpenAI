using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class IsoLanguageDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public IsoLanguageDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        return CultureInfo.GetCultures(CultureTypes.NeutralCultures)
            .Where(c => c.Name.Length >= 2)
            .Where(c => context.SearchString == null 
                        || c.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)
                        || c.EnglishName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .GroupBy(c => c.Name.Substring(0, 2), StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToDictionary(g => g.Key, g => g.First().EnglishName);
    }
}