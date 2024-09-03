using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Models.Responses.Batch;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.OpenAI.DataSourceHandlers;

public class BatchDataSourceHandler(InvocationContext invocationContext)
    : BaseInvocable(invocationContext), IAsyncDataSourceHandler
{
    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var client = new OpenAIClient();
        var request = new OpenAIRequest("/batches?limit=100", Method.Get, InvocationContext.AuthenticationCredentialsProviders);
        var batches = await client.ExecuteWithErrorHandling<BatchPaginationResponse>(request);
        var modelsDictionary = batches.Data
            .Where(model => context.SearchString == null || model.Id.Contains(context.SearchString))
            .ToDictionary(model => model.Id, model => model.Id);
        return modelsDictionary;
    }
}