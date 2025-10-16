using RestSharp;

namespace Apps.OpenAI.Api.Requests;

public class AzureOpenAiRequest : RestRequest
{
    public AzureOpenAiRequest(string endpoint, Method method, string apiKey) : base(endpoint, method)
    {
        this.AddHeader("api-key", apiKey);
    }
}