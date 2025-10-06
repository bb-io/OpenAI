using Apps.OpenAI.Utils;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apps.OpenAI.Services;

public class ContentPromptBuilderService
{
    public string GetPostEditSystemPrompt()
    {
        return "## Localization Specialist\n" +
                "You are a professional translator/" +
                "Focus solely on improving translations: correct errors, enhance fluency, ensure terminology consistency. " +
                "Preserve all formatting and XML or HTML tags exactly. " +
                "For ambiguous text, select the most natural translation in context. " +
                "If there are any additional instructions then it's very important to apply them." +
                "Process each segment independently even when similar.";
    }

    public string GetProcessSystemPrompt()
    {
        return "## Localization Specialist\n" +
                "You are a professional translator/" +
                "Focus solely on translating the text: ensure accuracy, fluency, and consistency. " +
                "Preserve all formatting and XML or HTML tags exactly. " +
                "For ambiguous text, select the most natural translation in context. " +
                "If there are any additional instructions then it's very important to apply them." +
                "Process each segment independently even when similar.";
    }

    public string BuildPostEditUserPrompt(string sourceLanguage, string targetLanguage,
        Dictionary<string, Segment> batch, string? additionalPrompt, string? glossaryPrompt, List<Note>? notes)
    {
        return BuildUserPrompt(sourceLanguage, targetLanguage, batch, additionalPrompt, glossaryPrompt, true, notes);
    }

    public string BuildProcessUserPrompt(string sourceLanguage, string targetLanguage,
        Dictionary<string, Segment> batch, string? additionalPrompt, string? glossaryPrompt, List<Note>? notes)
    {
        return BuildUserPrompt(sourceLanguage, targetLanguage, batch, additionalPrompt, glossaryPrompt, false, notes);
    }

    private string BuildUserPrompt(string sourceLanguage, string targetLanguage,
        Dictionary<string, Segment> batch, string? additionalPrompt, string? glossaryPrompt, 
        bool isPostEdit, List<Note>? notes)
    {
        var jsonData = isPostEdit 
            ? JsonConvert.SerializeObject(batch.Select(x => new { Id = x.Key, Source = x.Value.GetSource(), Target = x.Value.GetTarget() }))
            : JsonConvert.SerializeObject(batch.Select(x => new { Id = x.Key, Source = x.Value.GetSource() }));
            
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

         if (!string.IsNullOrEmpty(additionalPrompt) || (notes != null && notes.Count > 0))
        {
            prompt.AppendLine();
            prompt.AppendLine("### ADDITIONAL REQUIREMENTS");
            prompt.AppendLine(additionalPrompt);
            foreach (var note in notes)
            {
                prompt.AppendLine($"{note.Category}: {note.Text}");
            }
        }

        prompt.AppendLine();
        prompt.AppendLine("### TRANSLATION UNITS");
        prompt.AppendLine(jsonData);

        return prompt.ToString();
    }
}