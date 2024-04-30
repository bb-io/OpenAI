using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.DataSourceHandlers;

public class AudioResponseFormatDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new()
        {
            { "mp3", "MP3" },
            { "opus", "OPUS" },
            { "aac", "AAC" },
            { "flac", "FLAC" }
        };
    }
}