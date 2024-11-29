using System;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using System.Collections.Generic;
using System.Linq;
using Apps.OpenAI.Constants;

namespace Apps.OpenAI.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "Developer API token",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredNames.OrganizationId),
                new(CredNames.ApiKey) { Sensitive = true }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {
        try
        {
            var organizationId = values.First(v => v.Key == CredNames.OrganizationId);
            var apiKey = values.First(v => v.Key == CredNames.ApiKey);
            return
            [
                new(organizationId.Key, organizationId.Value),
                new(apiKey.Key, apiKey.Value)
            ];
        }
        catch (InvalidOperationException)
        {
            throw new Exception("Organization ID and/or API key are not specified in connection.");
        }
    }
}