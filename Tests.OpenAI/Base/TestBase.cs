using Newtonsoft.Json;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Connections;
using Microsoft.Extensions.Configuration;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Tests.OpenAI.Base;

public class TestBase
{
    public List<IEnumerable<AuthenticationCredentialsProvider>> CredentialGroups { get; private set; }
    public List<InvocationContext> InvocationContexts { get; private set; }
    public FileManagementClient FileManagementClient { get; private set; }
    public TestContext? TestContext { get; set; }

    public TestBase()
    {
        InitializeCredentials();
        InitializeInvocationContext();
        InitializeFileManager();
    }

    public InvocationContext GetInvocationContext(string connectionType)
    {
        var context = InvocationContexts.FirstOrDefault(x => x.AuthenticationCredentialsProviders.Any(y => y.Value == connectionType));
        if (context == null)
            throw new Exception($"Invocation context was not found for this connection type: {connectionType}");
        else return context;
    }

    private void InitializeCredentials()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        CredentialGroups = config.GetSection("ConnectionDefinition")
            .GetChildren()
            .Select(section => ToCredentials(section.GetChildren().ToDictionary(child => child.Key, child => child.Value ?? string.Empty)))
            .ToList();
    }

    // Configs copied from the platform carry ConnectionPropertyGroup instead of Connection type,
    // so they go through the app's ConnectionDefinition exactly as the platform does.
    private static IEnumerable<AuthenticationCredentialsProvider> ToCredentials(Dictionary<string, string> values) =>
        values.ContainsKey(nameof(ConnectionPropertyGroup))
            ? new ConnectionDefinition().CreateAuthorizationCredentialsProviders(values)
            : values.Select(value => new AuthenticationCredentialsProvider(value.Key, value.Value));

    private void InitializeInvocationContext()
    {
        InvocationContexts = new List<InvocationContext>();
        foreach (var credentialGroup in CredentialGroups)
        {
            InvocationContexts.Add(new InvocationContext
            {
                AuthenticationCredentialsProviders = credentialGroup
            });
        }
    }

    private void InitializeFileManager()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var folderLocation = config.GetSection("TestFolder").Value;
        FileManagementClient = new FileManagementClient(folderLocation!);
    }
}
