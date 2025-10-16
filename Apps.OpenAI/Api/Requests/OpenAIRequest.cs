using RestSharp;

namespace Apps.OpenAI.Api.Requests;

public class OpenAIRequest : RestRequest
{
    public OpenAIRequest(string endpoint, Method method, string beta = null) : base(endpoint, method)
    {
        if (beta != null) this.AddHeader("OpenAI-Beta", beta);
    }
}