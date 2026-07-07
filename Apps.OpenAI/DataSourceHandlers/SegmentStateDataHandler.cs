using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Filters.Enums;

namespace Apps.OpenAI.DataSourceHandlers;

public class SegmentStateDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(SegmentState.Initial.Serialize(), "Initial or empty"),
        new(SegmentState.Translated.Serialize(), "Translated"),
        new(SegmentState.Reviewed.Serialize(), "Reviewed"),
        new(SegmentState.Final.Serialize(), "Final"),
    ];
}