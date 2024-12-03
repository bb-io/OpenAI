using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;

public abstract class BaseModelDataSourceHandler : BaseInvocable, IAsyncDataSourceItemHandler
{
    protected abstract Func<string, bool> ModelIdFilter { get; }
    
    protected BaseModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var client = new OpenAIClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new OpenAIRequest("/models", Method.Get);
        var models = await client.ExecuteWithErrorHandling<ModelsList>(request);
        return models.Data
            .Where(model => ModelIdFilter(model.Id))
            .Where(model => context.SearchString == null || model.Id.Contains(context.SearchString))
            .Select(model => new DataSourceItem(model.Id, model.Id));
    }
}