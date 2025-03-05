using System.Collections.Generic;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;

namespace Apps.OpenAI.Dtos;

public record ChatAudioMessageDto(string Role, List<ChatAudioMessageContentDto> Content) : BaseChatMessageDto(Role);

public abstract record ChatAudioMessageContentDto(string Type);

public record ChatAudioMessageTextContentDto(string Type, string Text) : ChatAudioMessageContentDto(Type);

public record ChatAudioMessageAudioContentDto(string Type, InputAudio input_audio) : ChatAudioMessageContentDto(Type);

public class InputAudio
{
    [JsonProperty("format")]
    public string Format { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; }
}