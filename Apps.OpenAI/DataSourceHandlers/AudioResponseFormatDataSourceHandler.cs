using System;
using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class AudioResponseFormatDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public AudioResponseFormatDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var formats = new List<string>
        {
            "mp3",
            "opus",
            "aac",
            "flac"
        };

        return formats
            .Where(format => context.SearchString == null || format.Contains(context.SearchString, 
                StringComparison.OrdinalIgnoreCase))
            .ToDictionary(format => format, format => format);
    }
}