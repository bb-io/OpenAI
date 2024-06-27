using System.Collections.Generic;
using Newtonsoft.Json;

namespace Apps.OpenAI.Dtos;

public record ChatCompletionDto(IEnumerable<ChatCompletionChoiceDto> Choices, UsageDto Usage);

public record ChatCompletionChoiceDto
{
    public ChatMessageDto Message { get; init; }

    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
}