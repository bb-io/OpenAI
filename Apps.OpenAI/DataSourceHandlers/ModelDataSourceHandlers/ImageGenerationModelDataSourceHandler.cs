using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class ImageGenerationModelDataSourceHandler(InvocationContext invocationContext) : BaseModelDataSourceHandler(invocationContext)
{
    protected override Func<string, bool> ModelIdFilter => id => id.Contains("dall") || id.Contains("gpt-image");
}