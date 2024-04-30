using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.OpenAI.DataSourceHandlers;

public class TemperatureDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.0f, 2.0f, 0.1f)
            .ToDictionary(t => t, t => t);
    }
}