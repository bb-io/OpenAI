using System;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json;

namespace Apps.OpenAI.Utils;

public class TryCatchHelper
{
    public static void TryCatch(Action action, string message)
    {
        try
        {
            action();
        }
        catch (JsonException ex)
        {
            throw new PluginApplicationException($"The JSON we received from Open AI is invalid (this could be due to hallucination or because the output is too long and got cut off). " +
                                                 $"Please try adding a retry policy to this action and/or lower the 'Bucket size' to avoid returning too long of a response.", ex);
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException($"Exception message: {ex.Message}. {message}");
        }
    }
}