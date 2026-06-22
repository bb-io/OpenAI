using System;
using System.Collections.Generic;
using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class TextChatModelDataSourceHandler(InvocationContext invocationContext)
    : BaseModelDataSourceHandler(invocationContext)
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

    protected override IEnumerable<ModelDto> SortModels(IEnumerable<ModelDto> models)
        => TextChatModelOrdering.Sort(models);
}
