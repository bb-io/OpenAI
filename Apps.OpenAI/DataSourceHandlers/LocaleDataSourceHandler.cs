using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class LocaleDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public LocaleDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var searchString = context.SearchString;
        
        if (string.IsNullOrEmpty(searchString))
            return GetCommonLocales();

        return GetLocales(searchString);
    }

    private Dictionary<string, string> GetCommonLocales()
    {
        return new()
        {
            { "zh-Hans-CN", "Chinese (Simplified, China)" },
            { "en-AU", "English (Australia)"},
            { "en-CA", "English (Canada)" },
            { "en-GB", "English (United Kingdom)" },
            { "en-US", "English (United States)" },
            { "fr-CA", "French (Canada)" },
            { "fr-FR", "French (France)" },
            { "de-DE", "German (Germany)" },
            { "hi-IN", "Hindi (India)" },
            { "it-IT", "Italian (Italy)" },
            { "ja-JP", "Japanese (Japan)" },
            { "pt-BR", "Portuguese (Brazil)" },
            { "es-MX", "Spanish (Mexico)" },
            { "es-ES", "Spanish (Spain)" }
        };
    }

    private Dictionary<string, string> GetLocales(string searchString)
    {
        return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Where(c => c.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) 
                        || c.DisplayName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .ToDictionary(c => c.Name, c => c.DisplayName);
    }
}