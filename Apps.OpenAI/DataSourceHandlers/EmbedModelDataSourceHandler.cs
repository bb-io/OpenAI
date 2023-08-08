using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class EmbedModelDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public EmbedModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var embedModels = new List<string>
        {
            "text-embedding-ada-002",
            "text-similarity-ada-001",
            "text-similarity-babbage-001",
            "text-similarity-curie-001",
            "text-similarity-davinci-001",
            "text-search-ada-doc-001",
            "text-search-ada-query-001",
            "text-search-babbage-doc-001",
            "text-search-babbage-query-001",
            "text-search-curie-doc-001",
            "text-search-curie-query-001",
            "text-search-davinci-doc-001",
            "text-search-davinci-query-001",
            "code-search-ada-code-001",
            "code-search-ada-text-001",
            "code-search-babbage-code-001",
            "code-search-babbage-text-001"
        };
        
        return embedModels
            .Where(m => context.SearchString == null || m.Contains(context.SearchString))
            .ToDictionary(m => m, m => m);
    }
}