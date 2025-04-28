using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class ImageGenerationModelDataSourceHandler : BaseModelDataSourceHandler
{
    protected override Func<string, bool> ModelIdFilter => id => id.Contains("dall") || id.Contains("gpt-image");

    public ImageGenerationModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}