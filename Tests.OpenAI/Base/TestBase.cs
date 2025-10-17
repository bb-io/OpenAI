using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Microsoft.Extensions.Configuration;

namespace Tests.OpenAI.Base;

public class TestBase
{
    public List<IEnumerable<AuthenticationCredentialsProvider>> CredentialGroups { get; private set; }
    public List<InvocationContext> InvocationContext { get; private set; }
    public FileManagementClient FileManagementClient { get; private set; }

    public TestBase()
    {
        InitializeCredentials();
        InitializeInvocationContext();
        InitializeFileManager();
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
        InvocationContext = new List<InvocationContext>();
        foreach (var credentialGroup in CredentialGroups)
        {
            InvocationContext.Add(new InvocationContext
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
