using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Batch;

public class GetQualityScoreBatchResultResponse : GetBatchResultResponse
{
    [Display("Average score")]
    public double AverageScore { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; }
}