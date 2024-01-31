using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class SpeechCreationModelDataSourceHandler : BaseModelDataSourceHandler
{
    protected override Func<string, bool> ModelIdFilter => id => id.StartsWith("tts");

    public SpeechCreationModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}