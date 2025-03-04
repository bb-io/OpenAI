using System.Collections.Generic;

namespace Apps.OpenAI.Dtos;

public record ChatAudioMessageDto(string Role, List<ChatAudioMessageContentDto> Content) : BaseChatMessageDto(Role);

public abstract record ChatAudioMessageContentDto(string Type);

public record ChatAudioMessageTextContentDto(string Type, string Text) : ChatAudioMessageContentDto(Type);

public record ChatAudioMessageAudioContentDto(string Type, AudioUrlDto Audio_url) : ChatAudioMessageContentDto(Type);

public record AudioUrlDto(string Url);