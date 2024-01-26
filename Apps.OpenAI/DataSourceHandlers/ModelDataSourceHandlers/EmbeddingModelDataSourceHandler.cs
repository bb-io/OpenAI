using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class EmbeddingModelDataSourceHandler : BaseModelDataSourceHandler
{
    protected override Func<string, bool> ModelIdFilter => id =>
        id.StartsWith("text-similarity") || id.Contains("embedding") || id.Contains("search");

    public EmbeddingModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}