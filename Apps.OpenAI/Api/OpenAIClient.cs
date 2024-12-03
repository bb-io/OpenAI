using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.OpenAI.Api;

public class OpenAIClient : BlackBirdRestClient
{
    protected override JsonSerializerSettings JsonSettings => 
        new() { MissingMemberHandling = MissingMemberHandling.Ignore };

    public OpenAIClient(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders) : base(new RestClientOptions
        { ThrowOnAnyError = false, BaseUrl = new Uri("https://api.openai.com/v1"), MaxTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds }) 
    {
        var key = authenticationCredentialsProviders.Get(CredNames.ApiKey).Value;
        this.AddDefaultHeader("Authorization", $"Bearer {key}");
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if (response.Content == null)
            throw new PluginApplicationException(response.ErrorMessage);

        var error = JsonConvert.DeserializeObject<ErrorDtoWrapper>(response.Content, JsonSettings);

        if (response.StatusCode == HttpStatusCode.NotFound && error.Error.Type == "invalid_request_error")
            throw new PluginMisconfigurationException("Model chosen is not suitable for this task. Please choose a compatible model.");
        
        return new PluginApplicationException(error?.Error?.Message ?? response.ErrorException.Message);
    }
    
    protected async Task<T> ExecuteLongTimeRequest<T>(RestRequest request) where T : class
    {
        throw new NotImplementedException();
    }
}