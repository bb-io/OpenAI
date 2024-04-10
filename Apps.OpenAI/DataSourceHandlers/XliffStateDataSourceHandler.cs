using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.OpenAI.DataSourceHandlers
{
    public class XliffStateDataSourceHandler : BaseInvocable, IDataSourceHandler
    {
        public XliffStateDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        public Dictionary<string, string> GetData(DataSourceContext context)
        {
            var status = new List<string>
        {
             "final",
             "needs-adaptation",
             "needs-l10n",
             "needs-review-adaptation",
             "needs-review-l10n",
             "needs-review-translation",
             "needs-translation",
             "new",
             "signed-off",
             "translated"
        };

            return status
                .Where(status => context.SearchString == null || status.Contains(context.SearchString,
                    StringComparison.OrdinalIgnoreCase))
                .ToDictionary(status => status, status => status);
        }

    }
}
