using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.Invocables;

public class OpenAIInvocable : BaseInvocable
{
    public AuthenticationCredentialsProvider[] Creds 
        => InvocationContext.AuthenticationCredentialsProviders.ToArray();

    public OpenAIInvocable(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}