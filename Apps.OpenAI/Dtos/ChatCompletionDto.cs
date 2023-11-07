using System.Collections.Generic;

namespace Apps.OpenAI.Dtos;

public record ChatCompletionDto(IEnumerable<ChatCompletionChoiceDto> Choices);

public record ChatCompletionChoiceDto(ChatMessageDto Message);