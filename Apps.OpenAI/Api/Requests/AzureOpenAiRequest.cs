using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System.Collections.Generic;
using System.Linq;

namespace Apps.OpenAI.Api.Requests;

public class AzureOpenAiRequest : RestRequest
{
    public AzureOpenAiRequest(
        string endpoint,
        Method method, 
        Dictionary<string, object> body, 
        IEnumerable<AuthenticationCredentialsProvider> credentials
    ) : base(endpoint, method)
    {
        string apiKey = credentials.First(x => x.KeyName == CredNames.ApiKey).Value;
        this.AddHeader("api-key", apiKey);

        string deployment = credentials.First(x => x.KeyName == CredNames.Deployment).Value;
        body["model"] = deployment;

        var json = JsonConvert.SerializeObject(body, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        });
        this.AddStringBody(json, DataFormat.Json);
    }
}