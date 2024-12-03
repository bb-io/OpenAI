using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.OpenAI.DataSourceHandlers
{
    public class AssistantsDataSourceHandler : BaseInvocable, IAsyncDataSourceItemHandler
    {
        private const string Beta = "assistants=v1";

        public AssistantsDataSourceHandler(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
        {
            var client = new OpenAIClient(InvocationContext.AuthenticationCredentialsProviders);
            var request = new OpenAIRequest("/assistants", Method.Get, Beta);
            request.AddQueryParameter("limit", 100);
            var assistants = await client.ExecuteWithErrorHandling<DataDto<AssistantDto>>(request);
            return assistants.Data
                .Where(assistant => context.SearchString == null || assistant.Name.Contains(context.SearchString))
                .Select(assistant => new DataSourceItem(assistant.Id, assistant.Name));
        }
    }
}
