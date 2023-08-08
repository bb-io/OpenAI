using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class ChatCompletionsModelDataSourceHandler : BaseInvocable, IDataSourceHandler
{
    public ChatCompletionsModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var chatCompletionsModels = new List<string>
        {
            "gpt-4",
            "gpt-4-0613",
            "gpt-4-32k",
            "gpt-4-32k-0613",
            "gpt-3.5-turbo",
            "gpt-3.5-turbo-0613",
            "gpt-3.5-turbo-16k",
            "gpt-3.5-turbo-16k-0613"
        };
        
        return chatCompletionsModels
            .Where(m => context.SearchString == null || m.Contains(context.SearchString))
            .ToDictionary(m => m, m => m);
    }
}