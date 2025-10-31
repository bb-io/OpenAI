using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.OpenAI.DataSourceHandlers.ModelDataSourceHandlers.Base;

public abstract class BaseModelDataSourceHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    protected abstract Func<string, bool> ModelIdFilter { get; }
    
    protected virtual IEnumerable<string> PriorityModels => Enumerable.Empty<string>();

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var client = new OpenAiUniversalClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new OpenAIRequest("/models", Method.Get);
        var models = await client.ExecuteWithErrorHandling<ModelsList>(request);
        
        var filteredModels = models.Data
            .Where(model => ModelIdFilter(model.Id))
            .Where(model => context.SearchString == null || model.Id.Contains(context.SearchString))
            .Select(model => new DataSourceItem(model.Id, model.Id))
            .ToList();

        var priorityModels = PriorityModels.ToList();
        var priorityItems = priorityModels
            .Where(id => filteredModels.Any(m => m.Value == id))
            .Select(id => new DataSourceItem(id, id));
        
        var otherItems = filteredModels
            .Where(item => !priorityModels.Contains(item.Value));

        return priorityItems.Concat(otherItems).Distinct();
    }
}