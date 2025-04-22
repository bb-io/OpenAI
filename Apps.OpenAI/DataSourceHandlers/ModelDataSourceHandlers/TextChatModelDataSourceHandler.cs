using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class TextChatModelDataSourceHandler : BaseModelDataSourceHandler
{
    protected override Func<string, bool> ModelIdFilter =>
        id => (id.Contains("gpt-4o") || id.Contains("o3")) && !id.Contains("vision") && !id.Contains("instruct");

    public TextChatModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}