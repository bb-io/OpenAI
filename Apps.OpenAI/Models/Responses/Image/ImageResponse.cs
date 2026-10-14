using Blackbird.Applications.Sdk.Common.Files;

using Apps.OpenAI.Dtos;
using Blackbird.Applications.Sdk.Common;

namespace Apps.OpenAI.Models.Responses.Image;

public class ImageResponse
{
    public FileReference Image { get; set; }

    [Display("Usage")]
    public UsageDto? Usage { get; set; }
}
