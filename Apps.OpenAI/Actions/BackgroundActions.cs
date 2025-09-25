using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Responses.Background;
using Apps.OpenAI.Models.Responses.Batch;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.OpenAI.Actions;

[ActionList("Background")]
public class BackgroundActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("Download background file", 
        Description = "Download content that was processed in the background. This action should be called after the background process is completed.")]
    public async Task<BackgroundContentResponse> DownloadContentFromBackground([ActionParameter] BackgroundDownloadRequest request)
    {
        var batchRequests = await GetBatchRequestsAsync(request.BatchId);
        var batchResponse = await GetBatchStatusAsync(request.BatchId);
        
        var originalFileStream = await fileManagementClient.DownloadAsync(request.TransformationFile);
        var content = await Transformation.Parse(originalFileStream, request.TransformationFile.Name);
        
        var totalSegments = content.GetSegments().Count();
        var updatedCount = 0;
        var processedCount = 0;
        var usageList = new List<UsageDto>();
        
        var stream = await fileManagementClient.DownloadAsync(request.TransformationFile);
        var transformation = await Transformation.Parse(stream, request.TransformationFile.Name);
        var backgroundType = transformation.MetaData.FirstOrDefault(x => x.Type == "background-type")?.Value;
        
        foreach (var batchRequest in batchRequests)
        {
            processedCount++;
            var segment = content.GetSegments().FirstOrDefault(s => s.Id == batchRequest.CustomId);
            if (segment == null)
            {
                throw new PluginApplicationException(
                    $"Segment with id {batchRequest.CustomId} not found in the content file.");
            }

            var newContent = batchRequest.Response.Body.Choices[0].Message.Content;
            if (segment.GetTarget() != newContent)
            {
                if (backgroundType == "translate")
                {
                    segment.State = SegmentState.Translated;
                    segment.SetTarget(newContent);
                    updatedCount++;
                } 
                else if (backgroundType == "edit")
                {
                    if (segment.GetTarget() != newContent)
                    {
                        segment.SetTarget(newContent);
                        updatedCount++;
                    }
                    
                    segment.State = SegmentState.Reviewed;
                } 
                else
                {
                    segment.SetTarget(newContent);
                    updatedCount++;
                }
            }
            
            if (batchRequest.Response.Body.Usage != null)
            {
                usageList.Add(new UsageDto
                {
                    PromptTokens = batchRequest.Response.Body.Usage.PromptTokens,
                    CompletionTokens = batchRequest.Response.Body.Usage.CompletionTokens,
                    TotalTokens = batchRequest.Response.Body.Usage.TotalTokens
                });
            }
        }
        
        FileReference resultFile;
        if (request.OutputFileHandling == "original")
        {
            var targetContent = content.Target();
            resultFile = await fileManagementClient.UploadAsync(
                targetContent.Serialize().ToStream(), 
                targetContent.OriginalMediaType, 
                targetContent.OriginalName);
        } 
        else if (request.OutputFileHandling == "xliff1")
        {
            var xliff1String = Xliff1Serializer.Serialize(content);
            resultFile = await fileManagementClient.UploadAsync(
                xliff1String.ToStream(), 
                MediaTypes.Xliff, 
                content.XliffFileName);
        }
        else
        {
            resultFile = await fileManagementClient.UploadAsync(
                content.Serialize().ToStream(), 
                MediaTypes.Xliff, 
                content.XliffFileName);
        }
        
        return new BackgroundContentResponse
        {
            File = resultFile,
            Usage = UsageDto.Sum(usageList),
            TotalSegmentsCount = totalSegments,
            ProcessedSegmentsCount = processedCount,
            UpdatedSegmentsCount = updatedCount,
            BatchStatus = batchResponse.Status
        };
    }
    
    #region Helper Methods
    
    private async Task<List<BatchRequestDto>> GetBatchRequestsAsync(string batchId)
    {
        var batch = await GetBatchStatusAsync(batchId);
    
        if (batch.Status != "completed")
        {
            throw new PluginApplicationException(
                $"The batch process is not completed yet. Current status: {batch.Status}");
        }
        
        if (batch.Status == "failed")
        {
            throw new PluginApplicationException(
                $"The batch process failed. Errors: {batch.Errors}");
        }

        var fileContentResponse = await Client.ExecuteWithErrorHandling(
            new OpenAIRequest($"/files/{batch.OutputFileId}/content", Method.Get));

        var batchRequests = new List<BatchRequestDto>();
        using var reader = new StringReader(fileContentResponse.Content!);
        while (await reader.ReadLineAsync() is { } line)
        {
            var batchRequest = JsonConvert.DeserializeObject<BatchRequestDto>(line);
            batchRequests.Add(batchRequest);
        }

        return batchRequests;
    }
    
    private async Task<BatchResponse> GetBatchStatusAsync(string batchId)
    {
        var getBatchRequest = new OpenAIRequest($"/batches/{batchId}", Method.Get);
        return await Client.ExecuteWithErrorHandling<BatchResponse>(getBatchRequest);
    }
    
    #endregion
}