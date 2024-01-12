using System;
using System.Net;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.OpenAI.Api;

public class OpenAIClient : BlackBirdRestClient
{
    protected override JsonSerializerSettings JsonSettings => 
        new() { MissingMemberHandling = MissingMemberHandling.Ignore };

    public OpenAIClient() : base(new RestClientOptions
        { ThrowOnAnyError = false, BaseUrl = new Uri("https://api.openai.com/v1"), MaxTimeout = TimeSpan.FromMinutes(10).Milliseconds }) { }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if (response.Content == null)
            throw new Exception(response.ErrorMessage);

        var error = JsonConvert.DeserializeObject<ErrorDtoWrapper>(response.Content, JsonSettings);

        if (response.StatusCode == HttpStatusCode.NotFound && error.Error.Type == "invalid_request_error")
            return new("Model chosen is not suitable for this task. Please choose a compatible model.");
        
        return new(error.Error.Message);
    }
}