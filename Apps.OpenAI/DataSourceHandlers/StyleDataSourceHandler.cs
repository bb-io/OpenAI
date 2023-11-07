using System;
using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class StyleDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public StyleDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var styles = new List<string>
        {
            "vivid",
            "natural"
        };

        return styles
            .Where(style => context.SearchString == null || style.Contains(context.SearchString, 
                StringComparison.OrdinalIgnoreCase))
            .ToDictionary(style => style, style => style);
    }
}