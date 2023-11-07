using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Extensions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.DataSourceHandlers;

public class ModelDataSourceHandler : BaseInvocable, IAsyncDataSourceHandler
{
    public ModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var client = InvocationContext.AuthenticationCredentialsProviders.CreateOpenAiServiceSdk();
        var models = await client.Models.ListModel(cancellationToken);
        var modelsDictionary = models.Models
            .Where(model => model.Owner != "openai-dev" && model.Owner != "openai-internal")
            .Where(model => context.SearchString == null || model.Id.Contains(context.SearchString))
            .OrderByDescending(model => model.CreatedTime)
            .ToDictionary(model => model.Id, model => model.Id);
        return modelsDictionary;
    }
}