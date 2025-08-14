using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Models.PostEdit;

public record BatchProcessingOptions(
    string ModelId,
    string SourceLanguage,
    string TargetLanguage,
    string? Prompt,
    FileReference? Glossary,
    bool FilterGlossary,
    int MaxRetryAttempts,
    int? MaxTokens,
    string? ReasoningEffort);