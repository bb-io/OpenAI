using System;
using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class QualityDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public QualityDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var qualities = new List<string>
        {
            "hd",
            "standard"
        };

        return qualities
            .Where(quality => context.SearchString == null || quality.Contains(context.SearchString, 
                StringComparison.OrdinalIgnoreCase))
            .ToDictionary(quality => quality, quality => quality);
    }
}