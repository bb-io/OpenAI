using Newtonsoft.Json;
using Apps.OpenAI.Constants;
using Microsoft.Extensions.Configuration;
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
            .Select(section =>
                section.GetChildren()
               .Select(child => new AuthenticationCredentialsProvider(child.Key, child.Value))
            )
            .ToList();
    }

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
