using System;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
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
    [PollingEvent("On batch finished", "Triggered when a batch status is set to completed")]
    public async Task<PollingEventResponse<DateMemory, BatchResponse>> OnBatchFinished(
        PollingEventRequest<DateMemory> request,
        [PollingEventParameter] BatchIdentifier identifier)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastPollingTime = DateTime.UtcNow
                }
            };
        }
        
        var getBatchRequest = new OpenAIRequest($"/batches/{identifier.BatchId}", Method.Get, Creds);
        var batch = await Client.ExecuteWithErrorHandling<BatchResponse>(getBatchRequest);
        return new()
        {
            FlyBird = batch.Status == "completed",
            Result = batch,
            Memory = new()
            {
                LastPollingTime = DateTime.UtcNow
            }
        };
    }
}