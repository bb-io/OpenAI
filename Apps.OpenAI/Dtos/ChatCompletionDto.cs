using System.Collections.Generic;
using Newtonsoft.Json;

namespace Apps.OpenAI.Dtos;

public class ChatCompletionDto
{
    public IEnumerable<ChatCompletionChoiceDto> Choices { get; set; } = [];

    public UsageDto Usage { get; set; } = UsageDto.Zero;

    public IEnumerable<UrlCitationDto> Citations { get; set; } = [];

    public IEnumerable<string> Sources { get; set; } = [];
}

public class ChatCompletionChoiceDto
{
    public ChatMessageDto Message { get; set; } = new("assistant", string.Empty);

    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; } = "stop";
}

public class UrlCitationDto
{
    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}