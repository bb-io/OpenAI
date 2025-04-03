using Apps.OpenAI.Dtos;

namespace Apps.OpenAI.Models.PostEdit;

public record class ChatCompletitionResult(ChatCompletionDto? ChatCompletion, bool Success, string? Error = null);