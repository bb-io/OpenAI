using System;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Responses.Batch;
using Apps.OpenAI.Polling.Models;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using RestSharp;

namespace Apps.OpenAI.Polling;

[PollingEventList]
public class BatchPollingList(InvocationContext invocationContext) : BaseActions(invocationContext, null!)
{
    [PollingEvent("On background job finished", "Triggered when an OpenAI batch job reaches a terminal state (completed/failed/cancelled).")]
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
        
        var getBatchRequest = new OpenAIRequest($"/batches/{identifier.BatchId}", Method.Get);
        var batch = await UniversalClient.ExecuteWithErrorHandling<BatchResponse>(getBatchRequest);
        var triggered = (batch.Status == "completed" || batch.Status == "failed" || batch.Status == "cancelled") && !request.Memory.Triggered;
        return new()
        {
            FlyBird = triggered,
            Result = batch,
            Memory = new()
            {
                LastPollingTime = DateTime.UtcNow,
                Triggered = triggered
            }
        };
    }
}