using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class CompletionsModelDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public CompletionsModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var completionsModels = new List<string>
        {
            "text-davinci-003",
            "text-davinci-002",
            "text-curie-001",
            "text-babbage-001",
            "text-ada-001"
        };
        
        return completionsModels
            .Where(m => context.SearchString == null || m.Contains(context.SearchString))
            .ToDictionary(m => m, m => m);
    }
}