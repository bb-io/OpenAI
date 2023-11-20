using Apps.OpenAI.Api;
using Apps.OpenAI.Invocables;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.Actions.Base;

public abstract class BaseActions : OpenAIInvocable
{
    protected readonly OpenAIClient Client;

    protected BaseActions(InvocationContext invocationContext) : base(invocationContext)
    {
        Client = new OpenAIClient();
    }
}