using RestSharp;

namespace Apps.OpenAI.Extensions;

public static class RestRequestExtensions
{
    public static RestRequest AddParameterIfNotNull(this RestRequest restRequest, string paramName, string? paramValue)
    {
        if (string.IsNullOrWhiteSpace(paramValue))
            return restRequest;

        restRequest.AddParameter(paramName, paramValue);
        return restRequest;
    }
}
