using Blackbird.Xliff.Utils.Models;

namespace Apps.OpenAI.Services.Abstract;

public interface IPromptBuilderService
{
    string GetSystemPrompt();
    
    string BuildUserPrompt(
        string sourceLanguage,
        string targetLanguage,
        TranslationUnit[] batch,
        string? additionalPrompt,
        string? glossaryPrompt);
}
