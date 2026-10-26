using System.Collections.Generic;
using System.Linq;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.OpenAI.Services;

public static class ContentPromptBuilderService
{
    public static string BuildGlossaryPrompt(IEnumerable<string> languages, string customInstructions = null)
    {
        var langsList = languages.ToList();
        if (langsList.Count == 0)
            throw new PluginMisconfigurationException("No languages found");

        string defaultInstructions =
            "Extract and list all the subject matter terminologies and proper nouns from the text inputted by the user. " +
            "Extract words and phrases, not full sentences.";
        string instructions = string.IsNullOrWhiteSpace(customInstructions) ? defaultInstructions : customInstructions;

        string contract =
            $"For each term, provide an entry for these language codes: {string.Join(", ", langsList)}. " +
            $"Return ONLY a JSON object of this exact structure: " +
            $"{{\"result\": [{{{string.Join(", ", langsList.Select(x => $"\"{x}\": \"\""))}}}]}}";

        return $"{instructions}\n\n{contract}";
    }
}
