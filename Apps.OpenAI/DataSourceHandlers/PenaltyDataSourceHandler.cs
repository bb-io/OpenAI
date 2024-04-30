using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class PenaltyDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(-2.0f, 2.0f, 0.1f)
            .ToDictionary(p => p, p => p);
    }
}