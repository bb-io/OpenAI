using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Apps.OpenAI.Api.Requests;

public class OpenAIRequest : RestRequest
{
    public OpenAIRequest(string endpoint, Method method, string beta = null) : base(endpoint, method)
    {
        if (beta != null) 
            this.AddHeader("OpenAI-Beta", beta);
    }

    public OpenAIRequest(string endpoint, Method method, Dictionary<string, object> body, string beta = null)
    : this(endpoint, method, beta)
    {
        if (body != null) { 
            var json = JsonConvert.SerializeObject(body, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            }); 
            this.AddStringBody(json, DataFormat.Json);
        }
    }
}