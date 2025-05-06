using Blackbird.Xliff.Utils.Models;

namespace Apps.OpenAI.Services.Abstract;

public interface IPromptBuilderService
{
    string GetPostEditSystemPrompt();

    string GetProcessSystemPrompt();
    
    string BuildPostEditUserPrompt(
        string sourceLanguage,
        string targetLanguage,
        TranslationUnit[] batch,
        string? additionalPrompt,
        string? glossaryPrompt);
    
    string BuildProcessUserPrompt(
        string sourceLanguage,
        string targetLanguage,
        TranslationUnit[] batch,
        string? additionalPrompt,
        string? glossaryPrompt);
}
