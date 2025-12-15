using System;
using System.Collections.Generic;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class TextChatModelDataSourceHandler(InvocationContext invocationContext)
    : BaseModelDataSourceHandler(invocationContext)
{
    protected override IEnumerable<string> PriorityModels =>
    [
        "gpt-5.1",
        "gpt-5.1-codex-mini",
        "gpt-5.2",
        "gpt-5",
        "gpt-5-mini",
        "gpt-5-nano",
        "gpt-4.1",
        "gpt-4.1-nano",
        "gpt-4.1-mini",
        "o3",
        "o4-mini"
    ];

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
}