using System.Collections.Generic;
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

    [Display("Completion window"), JsonProperty("completion_window")]
    public string CompletionWindow { get; set; } = string.Empty;
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