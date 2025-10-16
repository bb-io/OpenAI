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
using System.Linq;
using System.Threading.Tasks;

namespace Apps.OpenAI.Api.Clients;

public class AzureOpenAiClient : BlackBirdRestClient, IOpenAiClient
{
    private readonly string _apiKey;
    private readonly string _deployment;

    public AzureOpenAiClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : base(
        new()
        {
            ThrowOnAnyError = false,
            BaseUrl = new Uri($"{credentials.Get(CredNames.Url).Value}/openai/v1"),
            MaxTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds
        }
    )
    {
        _apiKey = credentials.First(x => x.KeyName == CredNames.ApiKey).Value;
        _deployment = credentials.First(x => x.KeyName == CredNames.Deployment).Value;
    }

    public async ValueTask<ConnectionValidationResponse> ValidateConnection()
    {
        var request = new AzureOpenAiRequest("/chat/completions", Method.Post, _apiKey);
        request.AddBody(
            new
            {
                model = _deployment,
                messages = new []
                {
                    new
                    {
                        role = "user",
                        content = "hello world!"
                    }
                }
            }
        );

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
