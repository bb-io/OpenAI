using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.PostEdit;

public class OpenAiXliffInnerRequest
{
    public string ModelId { get; set; } = string.Empty;
    public string? Prompt { get; set; }
    public string? SystemPrompt { get; set; }
    public bool OverwritePrompts { get; set; } = false;
    public FileReference XliffFile { get; set; } = new();
    public FileReference? Glossary { get; set; } 
    public int BucketSize { get; set; } = 1500;
    public string? SourceLanguage { get; set; }
    public string? TargetLanguage { get; set; }
    public bool? PostEditLockedSegments { get; set; }
    public string? ProcessOnlyTargetState { get; set; }
    public bool? AddMissingTrailingTags { get; set; }
    public bool? FilterGlossary { get; set; }
    public bool NeverFail { get; set; } = false;
    public int? BatchRetryAttempts { get; set; } = 3;
    public int? MaxTokens { get; set; }
    public bool DisableTagChecks { get; set; }
}
