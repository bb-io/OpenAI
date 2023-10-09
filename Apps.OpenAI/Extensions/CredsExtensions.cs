using System.Collections.Generic;
using System.IO;
using System.Text;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAI.Extensions;
using OpenAI.Interfaces;

namespace Apps.OpenAI.Extensions;

public static class CredsExtensions
{
    public static IOpenAIService CreateOpenAiServiceSdk(
        this IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        var organization = creds.Get("Organization ID").Value;
        var apiKey = creds.Get("API key").Value;

        var connectionParams = new Dictionary<string, string>()
        {
            { "Organization", organization },
            { "ApiKey", apiKey }
        };
        var apiSettings = new Dictionary<string, Dictionary<string, string>>
        {
            { "OpenAIServiceOptions", connectionParams }
        };
        var apiSettingsJson = JsonConvert.SerializeObject(apiSettings);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(apiSettingsJson))).Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => configuration);
        serviceCollection.AddOpenAIService();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOpenAIService>();
    }
}