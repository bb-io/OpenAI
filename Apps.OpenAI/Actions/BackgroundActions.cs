using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api.Requests;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Models.Requests.Background;
using Apps.OpenAI.Models.Responses.Background;
using Apps.OpenAI.Models.Responses.Batch;
using Apps.OpenAI.Models.Responses.Batch.Error;
using Apps.OpenAI.Models.Responses.Review;
using Apps.OpenAI.Utils;
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        var content = await ErrorHandler.ExecuteWithErrorHandlingAsync(() =>
            Transformation.Parse(originalFileStream, request.TransformationFile.Name)
        );

        var units = content.GetUnits();
        var totalSegments = units.SelectMany(x => x.Segments).Count();
        var updatedCount = 0;
        var processedCount = 0;
        var usageList = new List<UsageDto>();
        
        var stream = await fileManagementClient.DownloadAsync(request.TransformationFile);
        var transformation = await Transformation.Parse(stream, request.TransformationFile.Name);
        var backgroundType = transformation.MetaData.FirstOrDefault(x => x.Type == "background-type")?.Value;

        var segments = backgroundType switch
        {
            "translate" => units.SelectMany(x => x.Segments).GetSegmentsForTranslation().ToList(),
            "edit" => units.SelectMany(x => x.Segments).GetSegmentsForEditing().ToList(),
            _ => units.SelectMany(x => x.Segments).Where(x => !x.IsIgnorbale).ToList()
        };

        foreach (var batchRequest in batchRequests)
        {
            var bucketIndex = int.TryParse(batchRequest.CustomId, out var idx) ? idx : 
                throw new PluginApplicationException($"Invalid CustomId '{batchRequest.CustomId}' in batch request. Expected an integer value. You probably provided the batch that was not created by Blackbird.");

            var responseContent = batchRequest.Response.Body.Choices[0].Message.Content;

            try
            {
                TranslationResponse translationResponse;
                var cleanedContent = responseContent.Trim().Trim('`').Trim();

                if (cleanedContent.StartsWith("{") || cleanedContent.StartsWith("["))
                {
                    translationResponse = JsonConvert.DeserializeObject<TranslationResponse>(cleanedContent)
                        ?? throw new PluginApplicationException($"Empty or invalid JSON in batch {bucketIndex}.");
                }
                else
                {
                    translationResponse = new TranslationResponse
                    {
                        Translations = new List<TranslationEntity>
                        {
                            new()
                            {
                                TranslationId = "0",
                                TranslatedText = cleanedContent
                            }
                        }
                    };
                }

                if (translationResponse.Translations == null || !translationResponse.Translations.Any())
                    throw new PluginApplicationException($"Invalid response format in batch {bucketIndex}. Expected translations array or plain text.");

                foreach (var translation in translationResponse.Translations)
                {
                    processedCount++;
                    var segmentIndex = int.TryParse(translation.TranslationId, out var segIdx) ? segIdx :
                        throw new PluginApplicationException($"Invalid translation_id '{translation.TranslationId}' in batch response. Expected an integer value.");

                    var segment = segments.Count > segmentIndex ? segments[segmentIndex] : null;
                    if (segment == null)
                    {
                        throw new PluginApplicationException($"Segment with index {segmentIndex} not found in the content file.");
                    }

                    var newContent = translation.TranslatedText;
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
                }
            }
            catch (JsonException ex)
            {
                throw new PluginApplicationException($"Failed to parse JSON response from batch {bucketIndex}: {ex.Message}. Response content: {responseContent}");
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
    
    [Action("Get background result", Description = "Get the MQM report results from a background batch process")]
    public async Task<MqmBackgroundResponse> GetMqmReportFromBackground(
        [ActionParameter] BackgroundDownloadRequest request)
    {
        var batchRequests = await GetBatchRequestsAsync(request.BatchId);
        var batchResponse = await GetBatchStatusAsync(request.BatchId);

        var stream = await fileManagementClient.DownloadAsync(request.TransformationFile);
        var content = await ErrorHandler.ExecuteWithErrorHandlingAsync(() =>
            Transformation.Parse(stream, request.TransformationFile.Name)
        );
        var units = content.GetUnits();
        var segments = units.SelectMany(x => x.Segments).Where(x => !x.IsIgnorbale && x.State == SegmentState.Translated).ToList();
        
        var usage = new UsageDto();
        var combinedReport = new StringBuilder();
        var segmentReports = new List<SegmentMqmReport>();

        foreach (var batchRequest in batchRequests)
        {
            var bucketIndex = int.TryParse(batchRequest.CustomId, out var idx)
                ? idx
                : throw new PluginApplicationException(
                    $"Invalid CustomId '{batchRequest.CustomId}' in batch request. Expected an integer value.");

            var responseContent = batchRequest.Response.Body.Choices[0].Message.Content;
            if (string.IsNullOrWhiteSpace(responseContent))
                throw new PluginApplicationException($"Empty response content in batch request {bucketIndex}.");

            try
            {
                var cleaned = responseContent.Trim().Trim('`').Trim();

                MqmReportResponse mqmResponse;

                if (cleaned.StartsWith("{") || cleaned.StartsWith("["))
                {
                    mqmResponse = JsonConvert.DeserializeObject<MqmReportResponse>(cleaned)
                        ?? throw new PluginApplicationException($"Invalid JSON MQM report format in batch {bucketIndex}.");

                    if (mqmResponse == null || mqmResponse.Reports == null)
                        mqmResponse = new MqmReportResponse();
                }
                else
                {
                    mqmResponse = new MqmReportResponse
                    {
                        Reports =
                        {
                            new MqmReportEntity
                            {
                                SegmentId = bucketIndex.ToString(),
                                MqmReport = cleaned
                            }
                        }
                    };
                }

                foreach (var report in mqmResponse.Reports)
                {
                    var segmentIndex = int.TryParse(report.SegmentId, out var segIdx)
                        ? segIdx
                        : throw new PluginApplicationException(
                            $"Invalid segment_id '{report.SegmentId}' in batch response. Expected an integer value.");

                    var segment = segments.Count > segmentIndex ? segments[segmentIndex] : null;
                    if (segment == null)
                    {
                        throw new PluginApplicationException($"Segment with index {segmentIndex} not found in the content file.");
                    }

                    var sourceText = segment.GetSource();
                    var targetText = segment.GetTarget();
                    var mqmReport = report.MqmReport;

                    combinedReport.AppendLine($"Source: {sourceText}");
                    combinedReport.AppendLine($"Translation: {targetText}");
                    combinedReport.AppendLine("MQM Report:");
                    combinedReport.AppendLine(mqmReport);
                    combinedReport.AppendLine(new string('-', 50));
                    combinedReport.AppendLine();

                    segmentReports.Add(new SegmentMqmReport
                    {
                        SourceText = sourceText,
                        TargetText = targetText,
                        MqmReport = mqmReport
                    });
                }
            }
            catch (JsonException ex)
            {
                throw new PluginApplicationException(
                    $"Failed to parse MQM report in batch {bucketIndex}: {ex.Message}. Response content: {responseContent}");
            }

            if (batchRequest.Response.Body.Usage != null)
            {
                usage += new UsageDto
                {
                    PromptTokens = batchRequest.Response.Body.Usage.PromptTokens,
                    CompletionTokens = batchRequest.Response.Body.Usage.CompletionTokens,
                    TotalTokens = batchRequest.Response.Body.Usage.TotalTokens
                };
            }
        }

        return new MqmBackgroundResponse
        {
            CombinedReport = combinedReport.ToString(),
            SegmentReports = segmentReports,
            Usage = usage,
            ProcessedSegments = segmentReports.Count,
            TotalSegments = segments.Count,
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

        if (string.IsNullOrEmpty(batch.OutputFileId) && !string.IsNullOrEmpty(batch.ErrorFileId))
        {
            var errorRequest = new OpenAIRequest($"/files/{batch.ErrorFileId}/content", Method.Get);
            var errorBatchResponse = await UniversalClient.ExecuteWithErrorHandling<BatchItemErrorResponse>(errorRequest);
            throw new PluginApplicationException(errorBatchResponse.Response.Body.Error.Message);
        }

        var fileContentRequest = new OpenAIRequest($"/files/{batch.OutputFileId}/content", Method.Get);
        var fileContentResponse = await UniversalClient.ExecuteWithErrorHandling(fileContentRequest);

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
        return await UniversalClient.ExecuteWithErrorHandling<BatchResponse>(getBatchRequest);
    }

    #endregion
}