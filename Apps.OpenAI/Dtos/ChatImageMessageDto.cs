using System.Collections.Generic;

namespace Apps.OpenAI.Dtos;

public record ChatImageMessageDto(string Role, List<ChatImageMessageContentDto> Content);

public abstract record ChatImageMessageContentDto(string Type);

public record ChatImageMessageTextContentDto(string Type, string Text) : ChatImageMessageContentDto(Type);

public record ChatImageMessageImageContentDto(string Type, ImageUrlDto Image_url) : ChatImageMessageContentDto(Type);

public record ImageUrlDto(string Url);