using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.DataSourceHandlers;

public class VoiceDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new Dictionary<string, string>
        {
            { "alloy", "Alloy" },
            { "echo", "Echo" },
            { "fable", "Fable" },
            { "onyx", "Onyx" },
            { "nova", "Nova" },
            { "shimmer", "Shimmer" },
        };
    }
}