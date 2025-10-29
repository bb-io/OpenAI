using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Services.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using TiktokenSharp;

namespace Apps.OpenAI.Services;

public class OpenAICompletionService(OpenAiUniversalClient openAIClient) : IOpenAICompletionService
{
    private const string DefaultEncoding = "cl100k_base";

    public async Task<ChatCompletitionResult> ExecuteChatCompletionAsync(
        IEnumerable<ChatMessageDto> messages,
        string modelId,
        BaseChatRequest request,
        object? responseFormat = null)
    {
        var jsonDictionary = new Dictionary<string, object>
        {
            { "model", modelId },
            { "messages", messages },
            { "top_p", request?.TopP ?? 1 },
            { "presence_penalty", request?.PresencePenalty ?? 0 },
            { "frequency_penalty", request?.FrequencyPenalty ?? 0 },
            { "response_format", responseFormat }
        };
        
        if(request?.Temperature != null && !modelId.Contains("gpt-5"))
        {
            jsonDictionary.Add("temperature", request.Temperature);
        }
        
        if (request?.MaximumTokens != null)
        {
            jsonDictionary.Add("max_completion_tokens", request.MaximumTokens);
        }
        
        if (request?.ReasoningEffort != null && modelId.Contains("gpt-5"))
        {
            jsonDictionary.Add("reasoning_effort", request.ReasoningEffort);
        }

        var jsonBodySerialized = JsonConvert.SerializeObject(jsonDictionary, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        });

        var apiRequest = new OpenAIRequest("/chat/completions", Method.Post)
            .AddJsonBody(jsonBodySerialized);

        var response = await openAIClient.ExecuteWithErrorHandling<ChatCompletionDto>(apiRequest);
        return new(response, true, null);
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
