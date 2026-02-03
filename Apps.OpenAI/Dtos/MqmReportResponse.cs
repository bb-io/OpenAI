using Newtonsoft.Json;
using System.Collections.Generic;
using Apps.OpenAI.Models.Entities;

namespace Apps.OpenAI.Dtos;

public class MqmReportResponse
{
    [JsonProperty("reports")]
    public List<MqmReportEntity> Reports { get; set; } = [];
}