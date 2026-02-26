using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Collections.Generic;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers;

public class AudioTranscriptionDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return
        [
            new DataSourceItem("whisper-1", "whisper-1"),
            new DataSourceItem("gpt-4o-transcribe", "gpt-4o-transcribe"),
            new DataSourceItem("gpt-4o-mini-transcribe", "gpt-4o-mini-transcribe"),
            new DataSourceItem("gpt-4o-mini-transcribe-2025-12-15", "gpt-4o-mini-transcribe-2025-12-15"),
            new DataSourceItem("gpt-4o-transcribe-diarize", "gpt-4o-transcribe-diarize")
        ];
    }
}