using Apps.OpenAI.Utils;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apps.OpenAI.Services;

public class ContentPromptBuilderService
{
    public string BuildSystemPrompt(string sourceLanguage, string targetLanguage, string? additionalPrompt, string? glossaryPrompt, bool isPostEdit, List<Note>? notes)
    {
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
        
        prompt.AppendLine("### OUTPUT FORMAT (STRICT)");
        prompt.AppendLine("You must respond ONLY with a valid JSON object. Do NOT include any conversational text, headers (like '### TRANSLATION UNITS') or explanations.");
        prompt.AppendLine("Do NOT wrap the JSON in Markdown formatting (e.g., no ```json blocks). Return the raw, parsable JSON string.");
        prompt.AppendLine("Your JSON must exactly match the following schema:");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"translations\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine("      \"translation_id\": \"<The exact ID provided in the input>\",");
        prompt.AppendLine("      \"translated_text\": \"<Your translated or edited text>\"");
        prompt.AppendLine("    }");
        prompt.AppendLine("  ]");
        prompt.AppendLine("}");
        prompt.AppendLine();

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

        return prompt.ToString();
    }

    public string BuildUserPrompt(Dictionary<string, Segment> batch, bool isPostEdit)
    {
        var jsonData = isPostEdit 
            ? JsonConvert.SerializeObject(batch.Select(x => new { Id = x.Key, Source = x.Value.GetSource(), Target = x.Value.GetTarget() }))
            : JsonConvert.SerializeObject(batch.Select(x => new { Id = x.Key, Source = x.Value.GetSource() }));            
       
        var prompt = new StringBuilder();
       
        prompt.AppendLine("### TRANSLATION UNITS");
        prompt.AppendLine(jsonData);

        return prompt.ToString();
    }
}