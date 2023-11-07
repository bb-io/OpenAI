using System.Collections.Generic;

namespace Apps.OpenAI.Dtos;

public record CompletionDto(IEnumerable<CompletionChoiceDto> Choices);

public record CompletionChoiceDto(string Text);