using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Constants;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Requests.Xliff;
using Apps.OpenAI.Models.Responses.Batch;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Xliff.Utils.Extensions;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.OpenAI.Actions;

[ActionList]
public class BatchActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : BaseActions(invocationContext, fileManagementClient)
{
    [Action("(Async) Process XLIFF file",
        Description =
            "Asynchronously process each translation unit in the XLIFF file according to the provided instructions (by default it just translates the source tags) and updates the target text for each unit. For now it supports only 1.2 version of XLIFF.")]
    public async Task<BatchResponse> ProcessXliffFileAsync(ProcessXliffFileRequest request)
    {
        var fileStream = await FileManagementClient.DownloadAsync(request.File);
        var xliffDocument = fileStream.ToXliffDocument();
        if (xliffDocument.TranslationUnits.Count == 0)
        {
            throw new InvalidOperationException("The XLIFF file does not contain any translation units.");
        }

        var requests = new List<object>();
        foreach (var translationUnit in xliffDocument.TranslationUnits)
        {
            var batchRequest = new
            {
                custom_id = translationUnit.Id,
                method = "POST",
                url = "/v1/chat/completions",
                body = new
                {
                    model = request.ModelId,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = SystemPromptConstants.ProcessXliffFile
                        },
                        new
                        {
                            role = "user",
                            content = translationUnit.Source
                        }
                    },
                    max_tokens = 1000
                }
            };

            requests.Add(batchRequest);
        }

        using var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);
        foreach (var requestObj in requests)
        {
            string json = JsonConvert.SerializeObject(requestObj);
            await streamWriter.WriteLineAsync(json);
        }

        await streamWriter.FlushAsync();
        memoryStream.Position = 0;
        var bytes = memoryStream.ToArray();

        var uploadFileRequest = new OpenAIRequest("/files", Method.Post, Creds)
            .WithFile(bytes, request.File.Name, "file")
            .AddParameter("purpose", "batch", ParameterType.RequestBody);
        var file = await Client.ExecuteWithErrorHandling<FileDto>(uploadFileRequest);

        var createBatchRequest = new OpenAIRequest("/batches", Method.Post, Creds)
            .WithJsonBody(new
            {
                input_file_id = file.Id,
                endpoint = "/v1/chat/completions",
                completion_window = "24h",
            });
        return await Client.ExecuteWithErrorHandling<BatchResponse>(createBatchRequest);
    }
}