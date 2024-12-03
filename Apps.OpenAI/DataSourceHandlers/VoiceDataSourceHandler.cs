using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.DataSourceHandlers;

public class VoiceDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>
        {
            new( "alloy", "Alloy" ),
            new( "echo", "Echo" ),
            new( "fable", "Fable" ),
            new( "onyx", "Onyx" ),
            new( "nova", "Nova" ),
            new( "shimmer", "Shimmer" ),
        };
    }
}