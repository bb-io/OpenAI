using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using System.Collections.Generic;
using System.Linq;

namespace Apps.OpenAI.Connections
{
    public class ConnectionDefinition : IConnectionDefinition
    {
        public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
        {
            new ConnectionPropertyGroup
            {
                Name = "Developer API token",
                AuthenticationType = ConnectionAuthenticationType.Undefined,
                ConnectionUsage = ConnectionUsage.Actions,
                ConnectionProperties = new List<ConnectionProperty>
                {
                    new("Organization ID"),
                    new("API key")
                }
            }
        };

        public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
            Dictionary<string, string> values)
        {
            var organizationId = values.First(v => v.Key == "Organization ID");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                organizationId.Key,
                organizationId.Value
            );

            var apiKey = values.First(v => v.Key == "API key");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                apiKey.Key,
                apiKey.Value
            );
        }
    }
}
