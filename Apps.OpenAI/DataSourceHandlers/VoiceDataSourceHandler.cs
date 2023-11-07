using System;
using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class VoiceDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public VoiceDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var voices = new List<string>
        {
            "alloy",
            "echo",
            "fable",
            "onyx",
            "nova",
            "shimmer"
        };

        return voices
            .Where(voice => context.SearchString == null || voice.Contains(context.SearchString, 
                StringComparison.OrdinalIgnoreCase))
            .ToDictionary(voice => voice, voice => voice);
    }
}