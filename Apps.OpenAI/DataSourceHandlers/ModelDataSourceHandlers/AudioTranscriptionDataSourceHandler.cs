using Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;
using Blackbird.Applications.Sdk.Common.Invocation;
using System;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class AudioTranscriptionDataSourceHandler(InvocationContext invocationContext) : BaseModelDataSourceHandler(invocationContext)
{
    protected override Func<string, bool> ModelIdFilter => id =>
        (id.StartsWith("whisper") || id.Contains("transcribe")) && !id.Equals("gpt-4o-mini-transcribe-2025-03-20", StringComparison.OrdinalIgnoreCase);
}