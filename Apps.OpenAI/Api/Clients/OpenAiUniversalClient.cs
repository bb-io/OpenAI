using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Apps.OpenAI.Api.Clients;

public class OpenAiUniversalClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : BlackBirdRestClient(CreateOptions(credentials))
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection()
    {
        string model = credentials.FirstOrDefault(x => x.KeyName == CredNames.Model)?.Value ?? "gpt-3.5-turbo";
        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = new[]
            {
                new
                {
                    role = "user",
                    content = "hello world!"
                }
            },
        };
        var request = new OpenAIRequest("/chat/completions", Method.Post, body);
        var (headerKey, headerValue) = GetAuthHeader(credentials);
        request.AddHeader(headerKey, headerValue);

        try
        {
            await base.ExecuteWithErrorHandling(request);
            return new() { IsValid = true };
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

    private (string headerKey, string headerValue) GetAuthHeader(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var connectionType = credentials.First(x => x.KeyName == CredNames.ConnectionType).Value;

        return connectionType switch
        {
            ConnectionTypes.AzureOpenAi => ("api-key", credentials.Get(CredNames.ApiKey).Value),
            ConnectionTypes.OpenAi => ("Authorization", $"Bearer {credentials.Get(CredNames.ApiKey).Value}"),
            ConnectionTypes.OpenAiEmbedded => ("Authorization", $"Bearer {credentials.Get(CredNames.ApiKey).Value}"),
            _ => throw new Exception($"Unsupported connection type: {connectionType}")
        };
    }

    private static RestClientOptions CreateOptions(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var connectionType = credentials.First(x => x.KeyName == CredNames.ConnectionType).Value;

        var baseUrl = connectionType switch
        {
            ConnectionTypes.AzureOpenAi => $"{credentials.Get(CredNames.Url).Value}/openai/v1",
            ConnectionTypes.OpenAi => "https://api.openai.com/v1",
            ConnectionTypes.OpenAiEmbedded => "https://api.openai.com/v1",
            _ => throw new Exception($"Connection type is not supported: {connectionType}")
        };

        return new RestClientOptions
        {
            ThrowOnAnyError = false,
            BaseUrl = new Uri(baseUrl),
            MaxTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds
        };
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        throw new NotImplementedException();
    }
}
