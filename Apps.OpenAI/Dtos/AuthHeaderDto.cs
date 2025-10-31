using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Apps.OpenAI.Dtos;

public class AuthHeaderDto
{
    public string HeaderKey { get; }

    public string HeaderValue { get; }

    public AuthHeaderDto(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        (string headerKey, string headerValue) = GetAuthHeader(credentials);
        HeaderKey = headerKey;
        HeaderValue = headerValue;
    }

    private static (string headerKey, string headerValue) GetAuthHeader(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var connectionType = credentials.First(x => x.KeyName == CredNames.ConnectionType).Value;

        return connectionType switch
        {
            ConnectionTypes.AzureOpenAi => ("api-key", credentials.Get(CredNames.ApiKey).Value),
            ConnectionTypes.OpenAi => ("Authorization", $"Bearer {credentials.Get(CredNames.ApiKey).Value}"),
            ConnectionTypes.OpenAiEmbedded => ("Authorization", $"Bearer {credentials.Get(CredNames.ApiKey).Value}"),
            _ => throw new Exception($"Unsupported connection type: {connectionType}")
        };
    }
}