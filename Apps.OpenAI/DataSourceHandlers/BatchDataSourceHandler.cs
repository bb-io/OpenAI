using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Models.Responses.Batch;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.OpenAI.DataSourceHandlers;

public class BatchDataSourceHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var client = new OpenAIClient(InvocationContext.AuthenticationCredentialsProviders);
        var request = new OpenAIRequest("/batches?limit=100", Method.Get);
        var batches = await client.ExecuteWithErrorHandling<BatchPaginationResponse>(request);
        return batches.Data
            .Where(model => context.SearchString == null || model.Id.Contains(context.SearchString))
            .Select(model => new DataSourceItem( model.Id, model.Id));
    }
}