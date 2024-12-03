using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.System;
using Microsoft.Extensions.Configuration;

namespace OpenAITests.Base
{
    public class TestBase
    {
        public IEnumerable<AuthenticationCredentialsProvider> Creds { get; set; }

        public InvocationContext InvocationContext { get; set; }

        public FileManager FileManager { get; set; }

        public TestBase()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            Creds = config.GetSection("ConnectionDefinition").GetChildren().Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();
            var folderLocation = config.GetSection("TestFolder").Value;

            InvocationContext = new InvocationContext
            {
                AuthenticationCredentialsProviders = Creds,
            };

            FileManager = new FileManager(folderLocation);
        }
    }
}
