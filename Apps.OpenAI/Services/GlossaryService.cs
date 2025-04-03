using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Apps.OpenAI.Services.Abstract;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils.Models;

namespace Apps.OpenAI.Services;

public class GlossaryService(IFileManagementClient fileManagementClient) : IGlossaryService
{
    public async Task<string?> BuildGlossaryPromptAsync(FileReference? glossary, IEnumerable<TranslationUnit> translationUnits, bool filter)
    {
        if(glossary == null)
        {
            return null;
        }

        var glossaryStream = await fileManagementClient.DownloadAsync(glossary);
        var blackbirdGlossary = await glossaryStream.ConvertFromTbx();

        var glossaryPromptPart = new StringBuilder();
        glossaryPromptPart.AppendLine("Glossary entries (each entry includes terms in different language. Each " +
                                      "language may have a few synonymous variations which are separated by ';'):");

        var entriesIncluded = false;
        var sourcesContent = filter ? translationUnits.Select(tu => tu.Source).Where(source => !string.IsNullOrEmpty(source)).ToList() : null;

        foreach (var entry in blackbirdGlossary.ConceptEntries)
        {
            var allTerms = entry.LanguageSections.SelectMany(x => x.Terms.Select(y => y.Term));
            if (filter && !IsEntryRelevantToSources(allTerms, sourcesContent))
            {
                continue;
            }
                
            entriesIncluded = true;
            glossaryPromptPart.AppendLine("-----------------");
            glossaryPromptPart.AppendLine("Entry:");

            foreach (var section in entry.LanguageSections)
            {
                glossaryPromptPart.AppendLine(
                    $"{section.LanguageCode}: {string.Join("; ", section.Terms.Select(term => term.Term))}");

                foreach (var term in section.Terms.Where(t => !string.IsNullOrEmpty(t.UsageExample)))
                {
                    glossaryPromptPart.AppendLine($"Usage example: {term.UsageExample}");
                }
            }
        }

        var glossaryPrompt = "Enhance the target text by incorporating relevant terms from our glossary where applicable. " +
                        "Ensure that the translation aligns with the glossary entries for the respective languages. " +
                        "If a term has variations or synonyms, consider them and choose the most appropriate " +
                        "translation to maintain consistency and precision.\n";
        return entriesIncluded ? glossaryPrompt + glossaryPromptPart.ToString() : null;
    }

    private bool IsEntryRelevantToSources(IEnumerable<string> terms, IEnumerable<string> sourcesContent)
    {
        return terms.Any(term => 
            sourcesContent.Any(source => 
                Regex.IsMatch(source, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase)));
    }
}
