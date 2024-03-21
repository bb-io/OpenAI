using System;
using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;

namespace Apps.OpenAI.Api;

public class OpenAIRequest : BlackBirdRestRequest
{
    public OpenAIRequest(string endpoint, Method method, 
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders, string? beta = null) 
        : base(endpoint, method, authenticationCredentialsProviders) 
    {
        if (beta != null) this.AddHeader("OpenAI-Beta", beta);
    }

    protected override void AddAuth(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders)
    {
        var apiKey = authenticationCredentialsProviders.First(p => p.KeyName == CredNames.ApiKey).Value;
        var organizationId = authenticationCredentialsProviders.First(p => p.KeyName == CredNames.OrganizationId).Value;
        this.AddHeader("Authorization", $"Bearer {apiKey}");
        this.AddHeader("OpenAI-Organization", organizationId);
    }
}