using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.DataSourceHandlers;

public class TimestampGranularitiesSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new()
        {
            { "word", "Word" },
            { "segment", "Segment" }
        };
    }
}