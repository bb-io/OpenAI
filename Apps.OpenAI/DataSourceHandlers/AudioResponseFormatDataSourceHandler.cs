using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.DataSourceHandlers;

public class AudioResponseFormatDataSourceHandler : IStaticDataSourceItemHandler
{    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>()
        {
            new( "mp3", "MP3" ),
            new( "opus", "OPUS" ),
            new( "aac", "AAC" ),
            new( "flac", "FLAC"),
        };
    }
}