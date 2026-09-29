using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class ImageEditingModelDataSourceHandler(InvocationContext invocationContext)
    : BaseModelDataSourceHandler(invocationContext)
{
    protected override Func<string, bool> ModelIdFilter =>
        id => id.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase);
}
