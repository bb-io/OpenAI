using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Apps.OpenAI.DataSourceHandlers
{
    public class XliffStateDataSourceHandler : IStaticDataSourceItemHandler
    {
        public IEnumerable<DataSourceItem> GetData()
        {
            return new List<DataSourceItem>
            {
                 new("final", "Final"),
                 new("needs-adaptation", "Needs adaptation"),
                 new("needs-l10n", "Needs l10n"),
                 new("needs-review-adaptation", "Needs review adaptation"),
                 new("needs-review-l10n", "Needs review l10n"),
                 new("needs-review-translation", "Needs review translation"),
                 new("needs-translation", "Needs translation"),
                 new("new", "New"),
                 new("signed-off", "Signed off"),
                 new("translated", "Translated"),
            };
        }
    }
}
