using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Api;

namespace Apps.OpenAI.Connections
{
    public class ConnectionValidator : IConnectionValidator
    {
        public async ValueTask<ConnectionValidationResponse> ValidateConnection(
            IEnumerable<AuthenticationCredentialsProvider> authProviders, CancellationToken cancellationToken)
        {
            var client = new OpenAIClient();
            var request = new OpenAIRequest("/models", Method.Get, authProviders);

            try
            {
                await client.ExecuteWithErrorHandling(request);
                return new()
                {
                    IsValid = true
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsValid = false,
                    Message = ex.Message
                };
            }
        }
    }
}
