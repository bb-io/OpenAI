using RestSharp;
using System.Linq;
using Apps.OpenAI.Constants;
using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Authentication;

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
        this.AddJsonBody(body);
    }
}