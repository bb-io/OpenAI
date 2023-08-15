using System;
using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class EncodingDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public EncodingDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var encodings = new List<string>
        {
            "cl100k_base",
            "p50k_base"
        };

        return encodings
            .Where(e => context.SearchString == null || e.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(e => e, e => e);
    }
}