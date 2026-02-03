using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.OpenAI.Models.Responses.Batch;

public class BatchResponse
{
    [Display("Batch ID")]
    public string Id { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    [Display("Input file ID"), JsonProperty("input_file_id")]
    public string InputFileId { get; set; } = string.Empty;
    
    [Display("Output file ID"), JsonProperty("output_file_id")]
    public string OutputFileId { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    [JsonProperty("errors")]
    public ErrorListDto Errors { get; set; } = new ErrorListDto();

    [Display("Completion window"), JsonProperty("completion_window")]
    public string CompletionWindow { get; set; } = string.Empty;
    
    [JsonProperty("created_at")]
    public string CreatedAt { get; set; }
    
    [JsonProperty("expectedCompletionTime")]
    public string ExpectedCompletionTime { get; set; }

    [DefinitionIgnore]
    [JsonProperty("error_file_id")]
    public string ErrorFileId { get; set; } = string.Empty;
}

public class BatchPaginationResponse
{
    [JsonProperty("data")]
    public List<BatchResponse> Data { get; set; } = new ();

    [JsonProperty("first_id")]
    public string FirstId { get; set; } = string.Empty;
    
    [JsonProperty("last_id")]
    public string LastId { get; set; } = string.Empty;
    
    [JsonProperty("has_more")]
    public bool HasMore { get; set; }
}

public class ErrorListDto
{
    [JsonProperty("object")]
    public string Object { get; set; } = "list";

    [JsonProperty("data")]
    public List<ErrorDto> Data { get; set; } = new();

    public override string ToString()
    {
        var errors = Data.Select(x => $"({x.Code})Message: {x.Message};").ToList();
        return string.Join(" ", errors);
    }
}

public class ErrorDto
{
    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("param")]
    public string Param { get; set; } = string.Empty;

    [JsonProperty("line")]
    public int? Line { get; set; }

    [JsonProperty("input_file_id")]
    public string InputFileId { get; set; } = string.Empty;
}