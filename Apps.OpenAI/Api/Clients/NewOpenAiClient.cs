using Apps.OpenAI.Api.Interfaces;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apps.OpenAI.Api.Clients;

public class NewOpenAiClient : BlackBirdRestClient, IOpenAiClient
{
    private static readonly RestClientOptions options = new RestClientOptions
    { 
        ThrowOnAnyError = false, 
        BaseUrl = new Uri("https://api.openai.com/v1"), 
        MaxTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds 
    };

    public NewOpenAiClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : base(options)
    {
        var key = credentials.Get(CredNames.ApiKey).Value;
        this.AddDefaultHeader("Authorization", $"Bearer {key}");
    }

    public async ValueTask<ConnectionValidationResponse> ValidateConnection()
    {
        var request = new OpenAIRequest("/models", Method.Get);

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

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        throw new NotImplementedException();
    }
}
