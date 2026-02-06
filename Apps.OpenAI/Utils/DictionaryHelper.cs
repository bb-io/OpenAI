using System.Collections.Generic;

namespace Apps.OpenAI.Utils;

public static class DictionaryHelper
{
    public static Dictionary<string, object> AppendIfNotNull<T>(this Dictionary<string, object> dictionary, string key, T value)
    {
        if (value != null)
            dictionary[key] = value;
        return dictionary;
    }
}
