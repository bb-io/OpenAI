using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Services.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using TiktokenSharp;

namespace Apps.OpenAI.Services;

public class OpenAICompletionService(OpenAIClient openAIClient) : IOpenAICompletionService
{    
    private const string DefaultEncoding = "cl100k_base";

    private readonly Dictionary<string, int> _modelMaxTokens = new()
    {
        ["gpt-4-1106-preview"] = 128000,
        ["gpt-4-vision-preview"] = 128000,
        ["gpt-4"] = 8192,
        ["gpt-4-32k"] = 32768,
        ["gpt-3.5-turbo"] = 4096,
        ["gpt-3.5-turbo-16k"] = 16384
    };

    public async Task<ChatCompletitionResult> ExecuteChatCompletionWithRetryAsync(
        IEnumerable<ChatMessageDto> messages,
        string modelId,
        BaseChatRequest request,
        int maxBatchRetries = 3)
    {
        int attempts = 0;
        Exception? lastException = null;

        while (attempts < maxBatchRetries)
        {
            try
            {
                attempts++;
                var jsonBody = new
                {
                    model = modelId,
                    Messages = messages,
                    max_tokens = !modelId.Contains("o1") ? (int?)(request?.MaximumTokens ?? 4096) : null,
                    max_completion_tokens = modelId.Contains("o1") ? (int?)(request?.MaximumTokens ?? 4096) : null,
                    top_p = request?.TopP ?? 1,
                    presence_penalty = request?.PresencePenalty ?? 0,
                    frequency_penalty = request?.FrequencyPenalty ?? 0,
                    temperature = request?.Temperature ?? 1,
                    response_format = ResponseFormats.GetProcessXliffResponseFormat()
                };

                var jsonBodySerialized = JsonConvert.SerializeObject(jsonBody, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                });

                var apiRequest = new OpenAIRequest("/chat/completions", Method.Post);
                apiRequest.AddJsonBody(jsonBodySerialized);

                var response = await openAIClient.ExecuteWithErrorHandling<ChatCompletionDto>(apiRequest);
                return new(response, true, null);
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempts < maxBatchRetries)
                {
                    await Task.Delay(CalculateBackoffDelay(attempts));
                }
            }
        }

        return new(null, false, lastException?.Message ?? "Maximum retry attempts reached");
    }

    public int CalculateTokenCount(string text, string modelId)
    {
        try
        {
            var encoding = GetEncodingForModel(modelId);
            var tikToken = TikToken.EncodingForModel(encoding);
            return tikToken.Encode(text).Count;
        }
        catch (Exception)
        {
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }

    public int GetModelMaxTokens(string modelId)
    {
        if (_modelMaxTokens.TryGetValue(modelId, out var tokens))
        {
            return tokens;
        }
        
        return 4096;
    }

    private TimeSpan CalculateBackoffDelay(int attempt)
    {
        var baseDelayMs = 1000 * Math.Pow(2, attempt);
        var jitter = new Random().NextDouble() * 0.3 + 0.85;
        return TimeSpan.FromMilliseconds(baseDelayMs * jitter);
    }

    private string GetEncodingForModel(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            return DefaultEncoding;
        }
            
        modelId = modelId.ToLower();
        if (modelId.StartsWith("gpt-4") || modelId.StartsWith("gpt-3.5") || modelId.StartsWith("text-embedding"))
        {
            return "cl100k_base";
        }
            
        if (modelId.Contains("davinci") || modelId.Contains("curie") || 
            modelId.Contains("babbage") || modelId.Contains("ada"))
        {
            return "p50k_base";
        }
            
        return DefaultEncoding;
    }
}
