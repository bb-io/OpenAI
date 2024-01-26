using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class CompletionModelDataSourceHandler : BaseModelDataSourceHandler
{
    protected override Func<string, bool> ModelIdFilter => id =>
        id.StartsWith("text-ada") || id.StartsWith("text-babbage") || id.StartsWith("text-curie") ||
        id.StartsWith("text-davinci") || id.StartsWith("babbage") || id.StartsWith("davinci") ||
        id.Contains("instruct");

    public CompletionModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}