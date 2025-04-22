using System;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class TextChatModelDataSourceHandler : BaseModelDataSourceHandler
{
    protected override Func<string, bool> ModelIdFilter =>
        id => 
        !id.Contains("vision") && 
        !id.Contains("instruct") && 
        !id.Contains("tts") && 
        !id.Contains("text-similarity") &&
        !id.Contains("embedding") && 
        !id.Contains("search") &&
        !id.Contains("dall") &&
        !id.Contains("whisper") &&
        !id.Contains("davinici");

    public TextChatModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}