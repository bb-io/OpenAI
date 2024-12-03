using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;

namespace Apps.OpenAI.Api;

public class OpenAIRequest : RestRequest
{
    public OpenAIRequest(string endpoint, Method method, string? beta = null) 
        : base(endpoint, method) 
    {
        if (beta != null) this.AddHeader("OpenAI-Beta", beta);
    }
}