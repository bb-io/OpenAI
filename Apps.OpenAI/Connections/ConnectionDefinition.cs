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
                    new("organizationId"),
                    new("apiKey")
                }
            }
        };

        public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
            Dictionary<string, string> values)
        {
            var organizationId = values.First(v => v.Key == "organizationId");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                organizationId.Key,
                organizationId.Value
            );

            var apiKey = values.First(v => v.Key == "apiKey");
            yield return new AuthenticationCredentialsProvider(
                AuthenticationCredentialsRequestLocation.None,
                apiKey.Key,
                apiKey.Value
            );
        }
    }
}
