using System.Collections.Generic;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Apps.OpenAI.Dtos;

public record ChatAudioMessageDto(string Role, List<ChatAudioMessageContentDto> Content) : BaseChatMessageDto(Role); //TODO rework this model

public abstract record ChatAudioMessageContentDto(string Type);

public record ChatAudioMessageTextContentDto(string Type, string Text) : ChatAudioMessageContentDto(Type);

public record ChatAudioMessageAudioContentDto(string Type, AudioData Data, AudioFormat Format ) : ChatAudioMessageContentDto(Type);

public record AudioData(string Type, string Base64);

public record AudioFormat(string Format);