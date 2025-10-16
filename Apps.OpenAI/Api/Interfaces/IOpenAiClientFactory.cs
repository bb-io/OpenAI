using Blackbird.Applications.Sdk.Common.Authentication;
using System.Collections.Generic;

namespace Apps.OpenAI.Api.Interfaces;

public interface IOpenAiClientFactory
{
    IOpenAiClient Create(IEnumerable<AuthenticationCredentialsProvider> credentials);
}
