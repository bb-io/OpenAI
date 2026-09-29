using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.OpenAI.DataSourceHandlers;

public class ImageLocalizationOutputFormatDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return
        [
            new("xliff2", "Interoperable XLIFF 2.2 (default)"),
            new("xliff1", "XLIFF 1.2")
        ];
    }
}
