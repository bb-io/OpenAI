using System;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Ai;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Responses.Batch;
using Apps.OpenAI.Polling.Models;
using Blackbird.Ai.Llm.Models;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.OpenAI.Polling;

[PollingEventList]
public class BatchPollingList(InvocationContext invocationContext) : BaseActions(invocationContext, null!)
{
    [PollingEvent("On background job finished", "Triggered once when a batch job reaches a terminal state (completed, failed, expired or cancelled).")]
    public async Task<PollingEventResponse<BatchMemory, BatchResponse>> OnBatchFinished(
        PollingEventRequest<BatchMemory> request,
        [PollingEventParameter] BatchIdentifier identifier)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastPollingTime = DateTime.UtcNow,
                    Triggered = false
                }
            };
        }

        // A background action with nothing to process creates no batch and returns an empty batch ID.
        var batch = string.IsNullOrWhiteSpace(identifier.BatchId)
            ? new BatchResponse { Status = "completed" }
            : await UniversalClient.GetBatchAsync(identifier.BatchId);
        var finished = OpenAiBatchLlmClient.StateOf(batch.Status) != BatchJobState.Running;

        return new()
        {
            FlyBird = finished && !request.Memory.Triggered,
            Result = batch,
            Memory = new()
            {
                LastPollingTime = DateTime.UtcNow,
                Triggered = request.Memory.Triggered || finished
            }
        };
    }
}
