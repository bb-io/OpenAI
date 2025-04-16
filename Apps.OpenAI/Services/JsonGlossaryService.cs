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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Apps.OpenAI.Services;

public class JsonGlossaryService(IFileManagementClient fileManagementClient) : IGlossaryService
{
    private class GlossaryTermEntry
    {
        public Dictionary<string, string> Terms { get; set; } = new();
        public string? UsageExample { get; set; }
    }

    private class GlossaryJson
    {
        public List<GlossaryTermEntry> ConceptEntries { get; set; } = new();
    }

    public async Task<string?> BuildGlossaryPromptAsync(FileReference? glossary, IEnumerable<TranslationUnit> translationUnits, bool filter)
    {
        if(glossary == null)
        {
            return null;
        }

        var glossaryStream = await fileManagementClient.DownloadAsync(glossary);
        var blackbirdGlossary = await glossaryStream.ConvertFromTbx();

        var jsonGlossary = new GlossaryJson();
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
            var glossaryEntry = new GlossaryTermEntry();
            
            foreach (var section in entry.LanguageSections)
            {
                if (section.Terms.Any())
                {
                    glossaryEntry.Terms[section.LanguageCode] = string.Join("; ", section.Terms.Select(term => term.Term));
                }
            }
            
            var usageExample = entry.LanguageSections
                .SelectMany(section => section.Terms)
                .FirstOrDefault(term => !string.IsNullOrEmpty(term.UsageExample))?.UsageExample;
                
            if (!string.IsNullOrEmpty(usageExample))
            {
                glossaryEntry.UsageExample = usageExample;
            }
            
            jsonGlossary.ConceptEntries.Add(glossaryEntry);
        }

        if (!entriesIncluded)
        {
            return null;
        }

        var jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };
        
        var jsonString = JsonConvert.SerializeObject(jsonGlossary, jsonSettings);
        
        var glossaryPrompt = new StringBuilder();
        glossaryPrompt.AppendLine("The following glossary is provided in JSON format to assist with translation. Each concept entry contains:");
        glossaryPrompt.AppendLine("- Terms in different languages (language codes as keys)");
        glossaryPrompt.AppendLine("- Optional usage examples to illustrate context");
        glossaryPrompt.AppendLine("Please use the most appropriate term from the glossary when translating, and maintain consistency with the provided terms.");
        glossaryPrompt.AppendLine();
        glossaryPrompt.AppendLine("Glossary:");
        glossaryPrompt.AppendLine(jsonString);
        
        return glossaryPrompt.ToString();
    }

    private bool IsEntryRelevantToSources(IEnumerable<string> terms, IEnumerable<string> sourcesContent)
    {
        return terms.Any(term => 
            sourcesContent.Any(source => 
                Regex.IsMatch(source, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase)));
    }
}
