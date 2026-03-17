using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.PostEdit;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Services.Abstract;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TiktokenSharp;

namespace Apps.OpenAI.Services;

public class OpenAICompletionService(OpenAiUniversalClient openAIClient) : IOpenAICompletionService
{
    private const string DefaultEncoding = "cl100k_base";

    public async Task<ChatCompletitionResult> ExecuteResponseAsync(
        IEnumerable<ChatMessageDto> messages,
        string modelId,
        BaseChatRequest request,
        object responseFormat = null)
    {
        var jsonDictionary = new Dictionary<string, object>
        {
            { "model", modelId },
            { "store", false },
            { "input", messages },
            { "top_p", request?.TopP ?? 1 },
        };

        if (responseFormat != null)
        {
            jsonDictionary.Add("text", new
            {
                format = responseFormat
            });
        }

        if (request?.Temperature != null)
        {
            jsonDictionary.Add("temperature", request.Temperature);
        }

        if (request?.MaximumTokens != null)
        {
            jsonDictionary.Add("max_output_tokens", request.MaximumTokens);
        }

        if (SupportsReasoningEffort(modelId) && !string.IsNullOrWhiteSpace(request?.ReasoningEffort))
        {
            jsonDictionary.Add("reasoning", new
            {
                effort = request.ReasoningEffort
            });
        }

        var response = await openAIClient.ExecuteApiRequest(jsonDictionary);
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

    private static string GetEncodingForModel(string modelId)
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

    private static bool SupportsReasoningEffort(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        var normalizedModel = modelId.Trim().ToLowerInvariant();
        return normalizedModel.StartsWith("gpt-5") || normalizedModel.StartsWith("o");
    }
}
