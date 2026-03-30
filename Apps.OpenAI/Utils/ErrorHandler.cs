using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Exceptions;
using System;
using System.Threading.Tasks;

namespace Apps.OpenAI.Utils;

public static class ErrorHandler
{
    public static async Task ExecuteWithErrorHandlingAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            if (IsError(ex))
                throw new PluginMisconfigurationException(MapErrorMessage(ex.Message));

            throw new PluginApplicationException(ex.Message);
        }
    }

    public static async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            if (IsError(ex))
                throw new PluginMisconfigurationException(MapErrorMessage(ex.Message));

            throw new PluginApplicationException(ex.Message);
        }
    }

    public static T ExecuteWithErrorHandling<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            if (IsError(ex))
                throw new PluginMisconfigurationException(MapErrorMessage(ex.Message));

            throw new PluginApplicationException(ex.Message);
        }
    }

    private static bool IsError(Exception ex)
    {
        return ex.Message.Contains("srcLang attribute is required", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("xliff", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Namespace Manager or XsltContext needed", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains(KnownErrorMessages.CouldNotDetectContentType)
            || ex.Message.Contains(KnownErrorMessages.CannotConvertToContent);
    }

    private static string MapErrorMessage(string message)
    {
        if (message == KnownErrorMessages.CouldNotDetectContentType)
            return "This file type is not supported";

        if (message == KnownErrorMessages.CannotConvertToContent)
            return "Cannot generate the original document format. The uploaded XLIFF is missing the original file's structural data";

        else return message;
    }
}
