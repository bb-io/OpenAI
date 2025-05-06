using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.Requests.Xliff;

public class TranslateXliffRequest
{
    public FileReference File { get; set; }

    [Display("Filter glossary terms")]
    public bool? FilterGlossary { get; set; }

    [Display("Update locked segments", Description = "By default it set to false. If true, OpenAI will not change the segments that are locked in the XLIFF file.")]
    public bool? UpdateLockedSegments { get; set; }
    
    [Display("Add missing trailing tags", Description = "If true, missing trailing tags will be added to the target segment.")]
    public bool? AddMissingTrailingTags { get; set; }

    [Display("Source language")]
    public string? SourceLanguage { get; set; }

    [Display("Target language")]
    public string? TargetLanguage { get; set; }

    [Display("Never fail", Description = "If set to true, the action will ignore any errors and return a result. If set to false, the action will fail if any errors occur. By default, this is set to false.")]
    public bool? NeverFail { get; set; }

    [Display("Batch retry attempts")]
    public int? BatchRetryAttempts { get; set; }

    [Display("Max tokens", Description = "The maximum number of tokens to generate in the completion. By default it is set to 1000.")]
    public int? MaxTokens { get; set; }

    [Display("Disable tag checks", Description = "After LLM provide the translation, it will be checked for tags. If the tags are not correct (model hallucinated), the translation of specific translation unit will be rejected. But disabling this option you highly increase the risk of hallucinations. By default it is set to false.")]
    public bool? DisableTagChecks { get; set; }
}