using System;

namespace Apps.OpenAI.Extensions;

public static class DataSourceHandlersExtensions
{
    public static string[] GenerateFormattedFloatArray(float start, float end, float step, string format = "0.0")
    {
        var length = (int)Math.Ceiling((end - start) / step) + 1;
        var result = new string[length];

        for (var i = 0; i < length; i++)
            result[i] = (start + i * step).ToString(format);

        return result;
    }
}