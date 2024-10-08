using System;

namespace Apps.OpenAI.Utils;

public class TryCatchHelper
{
    public static void TryCatch(Action action, string message)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            throw new Exception($"Exception message: {ex.Message}. {message}");
        }
    }
}