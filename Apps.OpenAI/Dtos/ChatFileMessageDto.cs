using System.Collections.Generic;
using Newtonsoft.Json;

namespace Apps.OpenAI.Dtos;

public record ChatFileMessageDto(
    [property: JsonProperty("role")] string Role,
    [property: JsonProperty("content")] List<object> Content
) : BaseChatMessageDto(Role);

public record ChatInputFileContentDto(
    [property: JsonProperty("type")] string Type,
    [property: JsonProperty("filename")] string FileName,
    [property: JsonProperty("file_data")] string FileData
);

public record ChatInputTextContentDto(
    [property: JsonProperty("type")] string Type,
    [property: JsonProperty("text")] string Text
);