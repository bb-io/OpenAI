using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Responses.Content;

public class ShortenContentResponse
{
    public FileReference File { get; set; } = new();

    [Display("Units with length limit")]
    public int UnitsWithLimit { get; set; }

    [Display("Units over length limit")]
    public int UnitsOverLimit { get; set; }

    [Display("Units shortened")]
    public int UnitsShortened { get; set; }

    [Display("Units still over length limit")]
    public int UnitsStillOverLimit { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; } = new();
}
