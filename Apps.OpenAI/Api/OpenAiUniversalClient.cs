using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Apps.OpenAI.Api;

public class OpenAiUniversalClient(IEnumerable<AuthenticationCredentialsProvider> credentials) : BlackBirdRestClient(CreateOptions(credentials))
{
    private readonly AuthHeaderDto _authHeader = new(credentials);

    public string ConnectionType => credentials.First(x => x.KeyName == CredNames.ConnectionType).Value;

    public async ValueTask<ConnectionValidationResponse> ValidateConnection()
    {
        string model = GetModel("gpt-3.5-turbo");
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

        try
        {
            await ExecuteChatCompletion(body);
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

    public async Task<ChatCompletionDto> ExecuteChatCompletion(Dictionary<string, object> input)
    {
        var request = new OpenAIRequest("/chat/completions", Method.Post, input);
        return await ExecuteWithErrorHandling<ChatCompletionDto>(request);
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;
        T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        return val == null ? throw new Exception($"Could not parse {content} to {typeof(T)}") : val;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        SetAuthHeader(request);

        RestResponse restResponse = await ExecuteAsync(request);
        if (!restResponse.IsSuccessStatusCode)
            throw ConfigureErrorException(restResponse);

        return restResponse;
    }

    public string GetModel(string defaultValue = null)
    {
        return ConnectionType switch
        {
            ConnectionTypes.OpenAiEmbedded =>
                defaultValue
                ?? credentials.FirstOrDefault(x => x.KeyName == CredNames.Model)?.Value
                ?? throw new PluginMisconfigurationException("Model must be provided in the input or connection"),

            ConnectionTypes.OpenAi =>
                defaultValue
                ?? throw new PluginMisconfigurationException("Model must be provided in the input"),

            ConnectionTypes.AzureOpenAi =>
                credentials.FirstOrDefault(x => x.KeyName == CredNames.Model)?.Value
                ?? throw new PluginMisconfigurationException("Model must be provided in the connection"),

            _ => throw new PluginApplicationException($"Unsupported connection type: {ConnectionType}")
        };
    }

    private void SetAuthHeader(RestRequest request) => request.AddHeader(_authHeader.HeaderKey, _authHeader.HeaderValue);

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
        return ErrorHelper.ConfigureErrorException(response, JsonSettings);
    }
}
