using Apps.OpenAI.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Xliff;

public class PromptXliffRequest
{
    public FileReference File { get; set; }

    [Display("Process only segments with this state", Description = "Only translation units with this value in the target state will be processed"), StaticDataSource(typeof(XliffStateDataSourceHandler))]
    public string? ProcessOnlyTargetState { get; set; }

    [Display("Add missing trailing tags", Description = "If true, missing trailing tags will be added to the target segment.")]
    public bool? AddMissingTrailingTags { get; set; }

    [Display("Never fail", Description = "If true, the request will never fail. Even with the critical error it will simply return the file you sent and the error messages. By default it is set to true.")]
    public bool? NeverFail { get; set; }

    [Display("Batch retry attempts", Description = "The number of attempts to retry the batch in case of failure. By default it is set to 2.")]
    public int? BatchRetryAttempts { get; set; }

    [Display("Disable tag checks", Description = "After LLM provide the translation, it will be checked for tags. If the tags are not correct (model hallucinated), the translation of specific translation unit will be rejected. But disabling this option you highly increase the risk of hallucinations. By default it is set to false.")]
    public bool? DisableTagChecks { get; set; }

    [Display("Filter glossary", Description = "By default, only glossary terms that appear in the source text will be included. Matching is exact (whole-term) and case-insensitive. Set to 'false' for including the whole glossary in prompt.")]
    public bool? FilterGlossary { get; set; }

    [Display("Modified by")]
    public string? ModifiedBy { get; set; }

    [Display("Update locked segments")]
    public bool? UpdateLockedSegments { get; set; }
}