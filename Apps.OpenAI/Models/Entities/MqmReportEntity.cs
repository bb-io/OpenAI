using Newtonsoft.Json;

namespace Apps.OpenAI.Models.Entities;

public class MqmReportEntity
{
    [JsonProperty("segment_id")]
    public string SegmentId { get; set; }

    [JsonProperty("mqm_report")]
    public string MqmReport { get; set; }
}