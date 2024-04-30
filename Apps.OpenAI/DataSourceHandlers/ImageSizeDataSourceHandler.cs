using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class ImageSizeDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new Dictionary<string, string>
        {
            { "256x256", "256x256 (dall-e-2)" },
            { "512x512", "512x512 (dall-e-2)" },
            { "1024x1024", "1024x1024" },
            { "1792x1024", "1792x1024 (dall-e-3)" },
            { "1024x1792", "1024x1792 (dall-e-3)" },
        };
    }
}