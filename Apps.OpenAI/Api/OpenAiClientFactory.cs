using System;
using System.Linq;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Api.Clients;
using Apps.OpenAI.Api.Interfaces;
using System.Collections.Generic;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.OpenAI.Api;

public class OpenAiClientFactory : IOpenAiClientFactory
{
    public IOpenAiClient Create(IEnumerable<AuthenticationCredentialsProvider> credentials)
    {
        var connectionType = credentials.FirstOrDefault(x => x.KeyName == CredNames.ConnectionType).Value;

        return connectionType switch
        {
            ConnectionTypes.AzureOpenAi => new AzureOpenAiClient(credentials),
            ConnectionTypes.OpenAi => new NewOpenAiClient(credentials),
            _ => throw new Exception($"Unknown connection type: {connectionType}")
        };
    }
}
