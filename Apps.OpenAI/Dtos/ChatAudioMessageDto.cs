using System.Collections.Generic;

namespace Apps.OpenAI.Dtos;

public record ChatAudioMessageDto(string Role, List<ChatAudioMessageContentDto> Content) : BaseChatMessageDto(Role); //TODO rework this model

public abstract record ChatAudioMessageContentDto(string Type);

public record ChatAudioMessageTextContentDto(string Type, string Text) : ChatAudioMessageContentDto(Type);

public record ChatAudioMessageAudioContentDto(string Type, InputAudio InputAudio) : ChatAudioMessageContentDto(Type);


public record InputAudio(AudioData Data, AudioFormat Format);

public record AudioData (string Base6);

public record AudioFormat(string Format);