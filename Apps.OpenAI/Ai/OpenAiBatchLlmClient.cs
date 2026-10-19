#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Responses.Batch;
using Blackbird.Ai.Llm.Abstract;
using Blackbird.Ai.Llm.Models;
using Newtonsoft.Json;

namespace Apps.OpenAI.Ai;

public sealed class OpenAiBatchLlmClient(OpenAiUniversalClient client, string? model, OpenAiLlmSettings settings) : IBatchLlmClient
{
    private const string MultipleModels = "multiple";

    private readonly object _gate = new();
    private string? _modelUsed;

    public LlmCapabilities Capabilities { get; } = OpenAiLlmClient.CapabilitiesFor(model, settings.MaxOutputTokens);

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

    public async Task<BatchJob> SubmitAsync(IReadOnlyList<BatchLlmRequest> requests, CancellationToken cancellationToken)
    {
        var resolvedModel = await client.ResolveTextChatModelAsync(model);

        var batch = await client.CreateBatchAsync(requests.Select(request => new
        {
            custom_id = request.CustomId,
            method = "POST",
            url = "/v1/responses",
            body = OpenAiLlmClient.Body(request.Request, resolvedModel, settings)
        }));

        return ToJob(batch);
    }

    public async Task<BatchJob> GetJobAsync(string jobId, CancellationToken cancellationToken) =>
        ToJob(await client.GetBatchAsync(jobId));

    // Successful lines are in the output file, rejected ones in the error file; an expired or
    // cancelled batch still has both for the requests it got to.
    public async IAsyncEnumerable<BatchLlmResult> GetResultsAsync(
        BatchJob job,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var batch = await client.GetBatchAsync(job.Id);

        foreach (var fileId in new[] { batch.OutputFileId, batch.ErrorFileId }.Where(id => !string.IsNullOrEmpty(id)))
        {
            using var reader = new StringReader(await client.GetFileContentAsync(fileId));

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    yield return ToResult(JsonConvert.DeserializeObject<BatchOutputLineDto>(line)!);
                }
            }
        }
    }

    public static BatchJobState StateOf(string status) => status switch
    {
        "completed" => BatchJobState.Completed,
        "expired" => BatchJobState.Expired,
        "cancelled" => BatchJobState.Cancelled,
        "failed" => BatchJobState.Failed,
        _ => BatchJobState.Running
    };

    private static BatchJob ToJob(BatchResponse batch) => new(
        batch.Id,
        StateOf(batch.Status),
        batch.RequestCounts?.Total ?? 0,
        batch.RequestCounts?.Completed ?? 0,
        batch.RequestCounts?.Failed ?? 0,
        batch.Errors?.Data.FirstOrDefault()?.Message,
        long.TryParse(batch.CreatedAt, out var createdAt) ? DateTimeOffset.FromUnixTimeSeconds(createdAt) : null);

    internal BatchLlmResult ToResult(BatchOutputLineDto line)
    {
        if (line.Error is not null)
        {
            return BatchLlmResult.Failed(line.CustomId, line.Error.Message ?? line.Error.Code ?? "Unknown error.");
        }

        if (line.Response is not { StatusCode: 200, Body: { } body })
        {
            var message = line.Response?.Body?.SelectToken("error.message")?.ToString();
            return BatchLlmResult.Failed(line.CustomId, message ?? $"The request failed with HTTP {line.Response?.StatusCode}.");
        }

        var completion = OpenAiUniversalClient.ToChatCompletion(body.ToObject<OpenAiResponseDto>()!);
        RecordModelUsed(completion.Usage.ModelUsed);

        return BatchLlmResult.Succeeded(line.CustomId, OpenAiLlmClient.ToLlmResponse(completion));
    }

    private void RecordModelUsed(string? reported)
    {
        if (string.IsNullOrWhiteSpace(reported))
        {
            return;
        }

        lock (_gate)
        {
            _modelUsed = _modelUsed is null || _modelUsed == reported ? reported : MultipleModels;
        }
    }
}
