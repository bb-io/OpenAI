using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Requests.Xliff;

public class GetQualityScoreBatchResultRequest : GetBatchResultRequest
{
    [Display("Throw error on any unexpected result")]
    public bool? ThrowExceptionOnAnyUnexpectedResult { get; set; }
}