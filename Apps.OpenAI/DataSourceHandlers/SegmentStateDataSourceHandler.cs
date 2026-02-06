using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Collections.Generic;

namespace Apps.OpenAI.DataSourceHandlers
{
    public class SegmentStateDataSourceHandler : IStaticDataSourceItemHandler
    {
        public IEnumerable<DataSourceItem> GetData()
        {
            return new List<DataSourceItem>
            {
                new("Initial", "Initial"),
                new("Translated", "Translated"),
                new("Reviewed", "Reviewed"),
                new("Final", "Final")
            };
        }
    }
}
