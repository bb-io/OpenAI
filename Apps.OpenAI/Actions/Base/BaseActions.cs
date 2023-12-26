using Apps.OpenAI.Api;
using Apps.OpenAI.Invocables;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.OpenAI.Actions.Base;

public abstract class BaseActions : OpenAIInvocable
{
    protected readonly OpenAIClient Client;
    protected readonly IFileManagementClient FileManagementClient;

    protected BaseActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) 
        : base(invocationContext)
    {
        Client = new OpenAIClient();
        FileManagementClient = fileManagementClient;
    }
}