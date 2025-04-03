using System.Collections.Generic;
using System.Threading.Tasks;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;

namespace Apps.OpenAI.Services.Abstract;

public interface IOpenAICompletionService
{
    Task<ChatCompletitionResult> ExecuteChatCompletionWithRetryAsync(
        IEnumerable<ChatMessageDto> messages, 
        string modelId, 
        BaseChatRequest request, 
        int maxBatchRetries = 3);
    int CalculateTokenCount(string text, string modelId);
    int GetModelMaxTokens(string modelId);
}