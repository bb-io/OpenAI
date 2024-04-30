using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class SpeedDataSourceHandler : IStaticDataSourceHandler
{

    public Dictionary<string, string> GetData()
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.25f, 4.0f, 0.05f, "0.00")
            .ToDictionary(speed => speed, speed => speed);
    }
}