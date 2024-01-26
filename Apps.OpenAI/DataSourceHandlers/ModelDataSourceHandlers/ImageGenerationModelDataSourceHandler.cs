using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class ImageGenerationModelDataSourceHandler : BaseModelDataSourceHandler
{
    protected override Func<string, bool> ModelIdFilter => id => id.StartsWith("dall");

    public ImageGenerationModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}