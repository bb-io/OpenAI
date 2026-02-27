using Newtonsoft.Json;
using System;

namespace Apps.OpenAI.Utils;

public class FlexibleIdConverter : JsonConverter<string>
{
    public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return reader.Value is null 
            ? string.Empty 
            : reader.Value.ToString();
    }

    public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }

    public override bool CanRead => true;
}