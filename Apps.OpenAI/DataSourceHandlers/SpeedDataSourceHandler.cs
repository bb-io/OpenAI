using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class SpeedDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public SpeedDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        return DataSourceHandlersExtensions.GenerateFormattedFloatArray(0.25f, 4.0f, 0.05f, "0.00")
            .Where(speed => context.SearchString == null || speed.Contains(context.SearchString))
            .ToDictionary(speed => speed, speed => speed);
    }
}