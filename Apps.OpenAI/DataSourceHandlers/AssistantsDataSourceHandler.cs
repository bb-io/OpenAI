using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Apps.OpenAI.DataSourceHandlers
{
    public class AssistantsDataSourceHandler : BaseInvocable, IAsyncDataSourceHandler
    {
        private const string Beta = "assistants=v1";

        protected AssistantsDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
            CancellationToken cancellationToken)
        {
            var client = new OpenAIClient();
            var request = new OpenAIRequest("/assistants", Method.Get, InvocationContext.AuthenticationCredentialsProviders, Beta);
            request.AddQueryParameter("limit", 100);
            var assistants = await client.ExecuteWithErrorHandling<DataDto<AssistantDto>>(request);
            var dictionary = assistants.Data
                .Where(assistant => context.SearchString == null || assistant.Name.Contains(context.SearchString))
                .ToDictionary(assistant => assistant.Id, assistant => assistant.Id);
            return dictionary;
        }
    }
}
