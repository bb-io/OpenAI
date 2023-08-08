using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class ImageSizeDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public ImageSizeDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var imageSizes = new List<string>
        {
            "256x256",
            "512x512",
            "1024x1024"
        };
        
        return imageSizes
            .Where(s => context.SearchString == null || s.Contains(context.SearchString))
            .ToDictionary(s => s, s => s);
    }
}