using System.Collections.Generic;
using System.Threading.Tasks;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;

namespace Apps.OpenAI.Services.Abstract;

public interface IOpenAICompletionService
{
    Task<ChatCompletitionResult> ExecuteChatCompletionAsync(
        IEnumerable<ChatMessageDto> messages, 
        string modelId, 
        BaseChatRequest request, 
        object? responseFormat = null);
    int CalculateTokenCount(string text, string modelId);
    int GetModelMaxTokens(string modelId);
}