using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Newtonsoft.Json.Linq;

namespace Apps.OpenAI.Ai;

public sealed class OpenAiLlmClient(OpenAiUniversalClient client, string? model, OpenAiLlmSettings settings) : ILlmClient
{
    private const string TruncatedFinishReason = "length";
    private const string MultipleModels = "multiple";

    // Resolved on the first request, so a file with nothing to process never calls the API.
    private readonly Lazy<Task<string>> _resolvedModel = new(() => client.ResolveTextChatModelAsync(model));
    private readonly object _gate = new();
    private string? _modelUsed;

    public LlmCapabilities Capabilities { get; } = CapabilitiesFor(model, settings.MaxOutputTokens);

    public string? ModelUsed
    {
        get
        {
            lock (_gate)
            {
                return _modelUsed;
            }
        }
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolvedModel = await _resolvedModel.Value;
        var completion = await client.ExecuteApiRequestAsync(Body(request, resolvedModel, settings));

        RecordModelUsed(string.IsNullOrWhiteSpace(completion.Usage.ModelUsed) ? resolvedModel : completion.Usage.ModelUsed);

        return ToLlmResponse(completion);
    }

    // Shared with OpenAiBatchLlmClient, so a batch line carries exactly the body a direct call sends.
    internal static Dictionary<string, object> Body(LlmRequest request, string resolvedModel, OpenAiLlmSettings settings)
    {
        var body = new Dictionary<string, object>
        {
            ["model"] = resolvedModel,
            ["store"] = false,
            ["input"] = new[]
            {
                new Dictionary<string, object> { ["role"] = MessageRoles.System, ["content"] = request.SystemPrompt },
                new Dictionary<string, object> { ["role"] = MessageRoles.User, ["content"] = request.UserPrompt }
            }
        };

        var openAiModel = new OpenAiModel(resolvedModel);

        if (settings.MaxOutputTokens is { } maxOutputTokens)
        {
            body["max_output_tokens"] = maxOutputTokens;
        }

        if (settings.Temperature is { } temperature)
        {
            body["temperature"] = temperature;
        }

        if (settings.TopP is { } topP && openAiModel.SupportsTopP(settings.ReasoningEffort))
        {
            body["top_p"] = topP;
        }

        if (openAiModel.SupportsReasoningEffort() && !string.IsNullOrWhiteSpace(settings.ReasoningEffort))
        {
            body["reasoning"] = new Dictionary<string, object> { ["effort"] = settings.ReasoningEffort };
        }

        if (request.ResponseSchema is { } schema)
        {
            body["text"] = new Dictionary<string, object>
            {
                ["format"] = new Dictionary<string, object>
                {
                    ["type"] = "json_schema",
                    ["name"] = schema.Name,
                    ["strict"] = true,
                    ["schema"] = JObject.Parse(schema.Schema)
                }
            };
        }

        return body;
    }

    internal static LlmResponse ToLlmResponse(ChatCompletionDto completion)
    {
        var choice = completion.Choices.First();

        return new LlmResponse(
            choice.Message.Content ?? string.Empty,
            new LlmUsage(completion.Usage.PromptTokens, completion.Usage.CompletionTokens),
            choice.FinishReason == TruncatedFinishReason ? FinishReason.Truncated : FinishReason.Stop);
    }

    private void RecordModelUsed(string reported)
    {
        lock (_gate)
        {
            _modelUsed = _modelUsed is null || _modelUsed == reported ? reported : MultipleModels;
        }
    }

    internal static LlmCapabilities CapabilitiesFor(string? model, int? requestedMaxOutputTokens)
    {
        var (contextTokens, maxOutputTokens) = LimitsFor(new OpenAiModel(model));

        return new LlmCapabilities(
            SupportsStructuredOutput: true,
            ContextTokens: contextTokens,
            MaxOutputTokens: requestedMaxOutputTokens is > 0
                ? Math.Min(requestedMaxOutputTokens.Value, maxOutputTokens)
                : maxOutputTokens);
    }

    // Unknown names, such as Azure deployment names, fall through to the conservative default.
    private static (int ContextTokens, int MaxOutputTokens) LimitsFor(OpenAiModel model)
    {
        var id = model.NormalizedId;

        if (id.StartsWith("gpt-5"))
        {
            return (400_000, 128_000);
        }

        if (id.StartsWith("gpt-4.1"))
        {
            return (1_047_576, 32_768);
        }

        if (model.IsOModel())
        {
            return (200_000, 100_000);
        }

        return (128_000, 16_384);
    }
}
