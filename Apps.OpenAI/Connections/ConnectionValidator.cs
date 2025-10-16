using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Api;

namespace Apps.OpenAI.Connections;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authProviders, CancellationToken cancellationToken)
    {
        var client = new OpenAiClientFactory().Create(authProviders);
        return await client.ValidateConnection();
    }
}
