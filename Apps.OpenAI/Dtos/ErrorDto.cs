namespace Apps.OpenAI.Dtos;

public record ErrorDto(string Message, string Type, string? Code);

public record ErrorDtoWrapper(ErrorDto Error);