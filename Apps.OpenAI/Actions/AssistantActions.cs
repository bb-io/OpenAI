using Apps.OpenAI.Actions.Base;
using Apps.OpenAI.Api;
using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.Identifiers;
using Apps.OpenAI.Models.Requests.Chat;
using Apps.OpenAI.Models.Responses.Chat;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Apps.OpenAI.DataSourceHandlers;
using Apps.OpenAI.Models.Requests.Assistant;
using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using DocumentFormat.OpenXml.Office2010.Excel;
using Blackbird.Applications.Sdk.Common.Files;
using System.IO;

namespace Apps.OpenAI.Actions;

[ActionList]
public class AssistantActions : BaseActions
{
    private const string Beta = "assistants=v1";
    private List<string> InProgressStatusses = ["queued", "in_progress"];

    public AssistantActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
        : base(invocationContext, fileManagementClient)
    {
    }


    [Action("Chat with assistant", Description = "Send a chat message to a pre-configured assistant and get a response. It can optionally take up to 10 files as input. Read docs for more details.")]
    public async Task<ChatResponse> ExecuteRun([ActionParameter] RunRequest input, [ActionParameter] TextChatModelIdentifier modelIdentifier)
    {
        var fileIds = new List<string>();
        if (input.Files != null)
        {
            foreach (var file in input.Files)
            {
                var res = await UploadFile(file);
                fileIds.Add(res.Id);
            }
        }

        var run = await StartRun(input, modelIdentifier, fileIds);

        while(InProgressStatusses.Contains(run.Status))
        {
            Console.WriteLine(run.Status);
            await Task.Delay(1000);
            run = await GetRun(run.ThreadId, run.Id);
        }

        var message = await GetThreadLastMessage(run.ThreadId);

        return new ChatResponse
        {
            Message = message,
        };
    }

    private async Task<string> GetThreadLastMessage(string threadId)
    {
        var request = new OpenAIRequest($"/threads/{threadId}/messages", Method.Get, Creds, Beta);
        var response = await Client.ExecuteWithErrorHandling<DataDto<AssistantResponseDto>>(request);
        if (response.Data.Count() < 2) throw new Exception("The assistant did not respond to the message.");
        var lastMessage = response.Data.FirstOrDefault();
        return lastMessage.Content.FirstOrDefault().Text.Value;
    }

    private Task<RunDto> GetRun(string threadId, string runId)
    {
        var request = new OpenAIRequest($"/threads/{threadId}/runs/{runId}", Method.Get, Creds, Beta);
        return Client.ExecuteWithErrorHandling<RunDto>(request);
    }

    private Task<RunDto> StartRun(RunRequest input, TextChatModelIdentifier modelIdentifier, List<string> fileIds)
    {
        var request = new OpenAIRequest("/threads/runs", Method.Post, Creds, Beta);

        var jsonBody = new
        {
            assistant_id = input.AssistantId,
            thread = new { messages = GenerateChatMessages(input.Message ?? string.Empty, fileIds) },
            model = modelIdentifier.ModelId,
        };

        var jsonBodySerialized = JsonConvert.SerializeObject(jsonBody, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        request.AddJsonBody(jsonBodySerialized);

        return Client.ExecuteWithErrorHandling<RunDto>(request);
    }

    private List<AssistantMessageDto> GenerateChatMessages(string content, List<string> fileIds)
    {
        var messages = new List<AssistantMessageDto>
        {
            new AssistantMessageDto { Role = "user", Content = content, FileIds = fileIds }
        };

        return messages;
    }

    private async Task<FileDto> UploadFile(FileReference file)
    {
        var request = new OpenAIRequest($"/files", Method.Post, Creds);
        var fileStream = await FileManagementClient.DownloadAsync(file);
        var fileBytes = await fileStream.GetByteData();
        request.AddFile("file", fileBytes, file.Name);
        request.AddParameter("purpose", "assistants");
        return await Client.ExecuteWithErrorHandling<FileDto>(request);
    }

    private async Task<FileReference> DownloadFile(string fileID)
    {
        var infoRequest = new OpenAIRequest($"/files/{fileID}", Method.Get, Creds);
        var infoResponse = await Client.ExecuteWithErrorHandling<FileDto>(infoRequest);

        var downloadRequest = new OpenAIRequest($"/files/{fileID}/content", Method.Get, Creds);
        var downloadResponse = await Client.ExecuteWithErrorHandling(downloadRequest);

        using var stream = new MemoryStream(downloadResponse.RawBytes);
        return await FileManagementClient.UploadAsync(stream, MimeTypes.GetMimeType(infoResponse.Filename), infoResponse.Filename);
    }
}
