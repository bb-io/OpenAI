using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Apps.OpenAI.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    private const string OpenAiConnectionName = "Developer API token";
    private const string AzureOpenAiConnectionName = "AzureConnection";

    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            DisplayName = "OpenAI",
            Name = OpenAiConnectionName,
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredNames.ApiKey) { Sensitive = true }
            }
        },
        new()
        {
            DisplayName = "Azure OpenAI",
            Name = AzureOpenAiConnectionName,
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredNames.Url) { DisplayName = "Resource URL" },
                new(CredNames.Deployment) { DisplayName = "Deployment name" },
                new(CredNames.ApiKey) { Sensitive = true }
            }
        },
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values)
    {
        var providers = values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();

        var connectionType = values[nameof(ConnectionPropertyGroup)] switch
        {
            AzureOpenAiConnectionName => ConnectionTypes.AzureOpenAi,
            OpenAiConnectionName => ConnectionTypes.OpenAi,
            _ => throw new Exception($"Unknown connection type: {values[nameof(ConnectionPropertyGroup)]}")
        };

        providers.Add(new AuthenticationCredentialsProvider(CredNames.ConnectionType, connectionType));
        return providers;
    }
}