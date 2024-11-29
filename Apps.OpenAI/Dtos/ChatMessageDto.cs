namespace Apps.OpenAI.Dtos;

public record BaseChatMessageDto(string Role);
public record ChatMessageDto(string Role, string Content) : BaseChatMessageDto(Role);