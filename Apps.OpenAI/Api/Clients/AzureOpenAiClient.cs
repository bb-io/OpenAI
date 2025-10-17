using Apps.OpenAI.Api.Interfaces;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apps.OpenAI.Api.Clients;

public class AzureOpenAiClient : BlackBirdRestClient, IOpenAiClient
{
    private readonly IEnumerable<AuthenticationCredentialsProvider> _credentials;

    public AzureOpenAiClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : base(
        new()
        {
            ThrowOnAnyError = false,
            BaseUrl = new Uri($"{credentials.Get(CredNames.Url).Value}/openai/v1"),
            MaxTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds
        }
    )
    {
        _credentials = credentials;
    }

    public async Task<ChatCompletionDto> ExecuteChatCompletion(Dictionary<string, object> input, string model)
    {
        var request = new AzureOpenAiRequest("/chat/completions", Method.Post, input, _credentials);
        return await base.ExecuteWithErrorHandling<ChatCompletionDto>(request);
    }

    public async ValueTask<ConnectionValidationResponse> ValidateConnection()
    {
        var body = new Dictionary<string, object>
        {
            ["messages"] = new[]
            {
                new
                {
                    role = "user",
                    content = "hello world!"
                }
            },
        };
        var request = new AzureOpenAiRequest("/chat/completions", Method.Post, body, _credentials);

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
        return ErrorHelper.ConfigureErrorException(response, JsonSettings);
    }
}
