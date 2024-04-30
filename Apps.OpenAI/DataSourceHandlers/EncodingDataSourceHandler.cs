using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.DataSourceHandlers;

public class EncodingDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new Dictionary<string, string>()
        {
            { "cl100k_base", "cl100k base" },
            { "p50k_base", "p50k base" }
        };
    }
}