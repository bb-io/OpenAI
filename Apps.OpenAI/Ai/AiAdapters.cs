using System;
using System.Threading.Tasks;
using Blackbird.Ai.Diagnostics;
using Blackbird.Ai.Diagnostics.Abstract;
using Blackbird.Ai.Exceptions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.OpenAI.Ai;

public static class AiAdapters
{
    // The library logs every processing issue as a warning, so actions do not return warnings as output.
    // The SDK logger's methods are delegates that may be unset, hence ?.Invoke.
    public static IAiLogger Logger(InvocationContext context) => new DelegateAiLogger(
        debug: message => context.Logger?.LogInformation?.Invoke(message, []),
        warn: message => context.Logger?.LogWarning?.Invoke(message, []),
        error: (message, exception) => context.Logger?.LogError?.Invoke($"{message} {exception}", []));

    // AiConfigurationException always means the user has to change an input.
    public static async Task<T> Mapped<T>(Func<Task<T>> run)
    {
        try
        {
            return await run();
        }
        catch (AiConfigurationException exception)
        {
            throw new PluginMisconfigurationException(exception.Message, exception);
        }
    }
}
