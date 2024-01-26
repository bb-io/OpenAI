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

public abstract class BaseModelDataSourceHandler : BaseInvocable, IAsyncDataSourceHandler
{
    protected abstract Func<string, bool> ModelIdFilter { get; }
    
    protected BaseModelDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var client = new OpenAIClient();
        var request = new OpenAIRequest("/models", Method.Get, InvocationContext.AuthenticationCredentialsProviders);
        var models = await client.ExecuteWithErrorHandling<ModelsList>(request);
        var modelsDictionary = models.Data
            .Where(model => ModelIdFilter(model.Id))
            .Where(model => context.SearchString == null || model.Id.Contains(context.SearchString))
            .ToDictionary(model => model.Id, model => model.Id);
        return modelsDictionary;
    }
}