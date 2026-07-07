using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Glossary;

public class ExtractGlossaryFromXliffRequest
{
    [Display("Content")] 
    public FileReference File { get; set; } = null!;

    [Display("Glossary name")]
    public string? Name { get; set; }
}