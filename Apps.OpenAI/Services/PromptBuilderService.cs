using System.Linq;
using System.Text;
using Apps.OpenAI.Services.Abstract;
using Apps.OpenAI.Utils;
using Blackbird.Xliff.Utils.Models;
using Newtonsoft.Json;

namespace Apps.OpenAI.Services;

public class PromptBuilderService : IPromptBuilderService
{
    public string GetPostEditSystemPrompt()
    {
        return "## Localization Specialist\n" +
                "You are a professional translator specializing in post-editing XLIFF translations. " +
                "Maintain a formal tone appropriate to content type. " +
                "Focus solely on improving translations: correct errors, enhance fluency, ensure terminology consistency. " +
                "Preserve all formatting and XML tags exactly. " +
                "For ambiguous text, select the most natural translation in context. " +
                "Process each segment independently even when similar.";
    }

    public string GetProcessSystemPrompt()
    {
        return "## Localization Specialist\n" +
                "You are a professional translator specializing in XLIFF translations. " +
                "Maintain a formal tone appropriate to content type. " +
                "Focus solely on translating the text: ensure accuracy, fluency, and consistency. " +
                "Preserve all formatting and XML tags exactly. " +
                "For ambiguous text, select the most natural translation in context. " +
                "Process each segment independently even when similar.";
    }

    public string BuildPostEditUserPrompt(string sourceLanguage, string targetLanguage,
        TranslationUnit[] batch, string? additionalPrompt, string? glossaryPrompt)
    {
        return BuildUserPrompt(sourceLanguage, targetLanguage, batch, additionalPrompt, glossaryPrompt, true);
    }

    public string BuildProcessUserPrompt(string sourceLanguage, string targetLanguage, 
        TranslationUnit[] batch, string? additionalPrompt, string? glossaryPrompt)
    {
        return BuildUserPrompt(sourceLanguage, targetLanguage, batch, additionalPrompt, glossaryPrompt, false);
    }

    private string BuildUserPrompt(string sourceLanguage, string targetLanguage,
        TranslationUnit[] batch, string? additionalPrompt, string? glossaryPrompt, 
        bool isPostEdit)
    {
        var jsonData = isPostEdit 
            ? JsonConvert.SerializeObject(batch.Select(x => new { x.Id, x.Source, x.Target }))
            : JsonConvert.SerializeObject(batch.Select(x => new { x.Id, x.Source }));
            
        var fullSourceLanguage = LanguageUtils.GetFullLanguageName(sourceLanguage);
        var fullTargetLanguage = LanguageUtils.GetFullLanguageName(targetLanguage);

        var prompt = new StringBuilder();

        prompt.AppendLine($"### TRANSLATION TASK");
        prompt.AppendLine($"You are a professional translator from {fullSourceLanguage} to {fullTargetLanguage}.");
        prompt.AppendLine();

        prompt.AppendLine("### INSTRUCTIONS");
        
        if (isPostEdit)
        {
            prompt.AppendLine("1. Review each translation unit containing source text and initial target translation");
            prompt.AppendLine("2. Edit the target text to ensure accuracy and fluency in the target language");
        }
        else
        {
            prompt.AppendLine("1. Translate the source text into the target language");
            prompt.AppendLine("2. Ensure the translation is accurate and fluent in the target language");
        }
        
        prompt.AppendLine("3. Preserve all XML tags exactly as they appear in the source text");
        prompt.AppendLine("4. Maintain consistent terminology throughout the translations");

        if (!string.IsNullOrEmpty(glossaryPrompt))
        {
            prompt.AppendLine();
            prompt.AppendLine("### TERMINOLOGY GUIDELINES");
            prompt.AppendLine(glossaryPrompt);
        }

        if (!string.IsNullOrEmpty(additionalPrompt))
        {
            prompt.AppendLine();
            prompt.AppendLine("### ADDITIONAL REQUIREMENTS");
            prompt.AppendLine(additionalPrompt);
        }

        prompt.AppendLine();
        prompt.AppendLine("### TRANSLATION UNITS");
        prompt.AppendLine(jsonData);

        return prompt.ToString();
    }
}