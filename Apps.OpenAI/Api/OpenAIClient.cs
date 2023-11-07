using System;
using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.OpenAI.Api;

public class OpenAIClient : BlackBirdRestClient
{
    protected override JsonSerializerSettings JsonSettings =>
        new() { MissingMemberHandling = MissingMemberHandling.Ignore,  };

    public OpenAIClient() : base(new RestClientOptions
        { ThrowOnAnyError = false, BaseUrl = new Uri("https://api.openai.com/v1") }) { }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var error = JsonConvert.DeserializeObject<ErrorDtoWrapper>(response.Content, JsonSettings);
        return new(error.Error.Message);
    }
}