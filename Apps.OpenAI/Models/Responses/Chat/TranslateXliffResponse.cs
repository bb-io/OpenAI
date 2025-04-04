using Apps.OpenAI.Dtos;
using Apps.OpenAI.Models.PostEdit;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Responses.Chat;

public class TranslateXliffResponse
{
    [Display("File")]
    public FileReference File { get; set; }

    [Display("Usage")]
    public UsageDto Usage { get; set; }
}