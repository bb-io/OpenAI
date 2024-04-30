using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.DataSourceHandlers;

public class QualityDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new Dictionary<string, string>()
        {
            { "hd", "HD" },
            { "standard", "Standard" }
        };        
    }
}