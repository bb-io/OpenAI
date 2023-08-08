using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class EditsModelDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public EditsModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var editsModels = new List<string>
        {
            "text-davinci-edit-001",
            "code-davinci-edit-001"
        };
        
        return editsModels
            .Where(m => context.SearchString == null || m.Contains(context.SearchString))
            .ToDictionary(m => m, m => m);
    }
}