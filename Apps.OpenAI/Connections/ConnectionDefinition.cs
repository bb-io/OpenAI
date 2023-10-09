using System;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using System.Collections.Generic;
using System.Linq;

namespace Apps.OpenAI.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "Developer API token",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionUsage = ConnectionUsage.Actions,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new("Organization ID"),
                new("API key") { Sensitive = true }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values)
    {
        try
        {
            var organizationId = values.First(v => v.Key == "Organization ID");
            var apiKey = values.First(v => v.Key == "API key");
            return new AuthenticationCredentialsProvider[]
            {
                new(AuthenticationCredentialsRequestLocation.None, organizationId.Key, organizationId.Value),
                new(AuthenticationCredentialsRequestLocation.None, apiKey.Key, apiKey.Value)
            };
        }
        catch (InvalidOperationException)
        {
            throw new Exception("Organization ID and/or API key are not specified in connection.");
        }
    }
}