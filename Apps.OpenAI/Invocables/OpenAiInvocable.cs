using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.Invocables;

public class OpenAiInvocable : BaseInvocable
{
    public AuthenticationCredentialsProvider[] Creds 
        => InvocationContext.AuthenticationCredentialsProviders.ToArray();

    public OpenAiInvocable(InvocationContext invocationContext) : base(invocationContext)
    {
    }
}