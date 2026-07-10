using System.Collections.Generic;
using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Glossary;

public class ExtractGlossaryFromXliffRequest
{
    [Display("Content")] 
    public FileReference File { get; set; } = null!;

    [Display("Glossary name")]
    public string? Name { get; set; }

    [Display("Segment states", Description = "Only extract terminology from segments in these states")]
    [StaticDataSource(typeof(SegmentStateDataHandler))]
    public IEnumerable<string>? SegmentStates { get; set; }

    [Display("Custom instructions", Description = "Replaces the default term-extraction instructions with your own")]
    public string? CustomInstructions { get; set; }
}