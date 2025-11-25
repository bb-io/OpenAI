using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Glossaries.Utils.Converters;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Apps.OpenAI.Services;

public class ContentGlossaryService(IFileManagementClient fileManagementClient)
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

    public async Task<string?> BuildGlossaryPromptAsync(FileReference? glossary, IEnumerable<Segment> segments, bool filter, bool? overwritePrompt = false)
    {
        if(glossary == null)
        {
            return null;
        }

        var glossaryStream = await fileManagementClient.DownloadAsync(glossary);

        using var sanitizedGlossaryStream = await glossaryStream.SanitizeTbxXmlAsync();

        var blackbirdGlossary = await sanitizedGlossaryStream.ConvertFromTbx();

        var jsonGlossary = new GlossaryJson();
        var entriesIncluded = false;
        var sourcesContent = filter ? segments.Select(seg => seg.GetSource()).Where(source => !string.IsNullOrEmpty(source)).ToList() : null;

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
            Formatting = Formatting.None
        };
        
        var jsonString = JsonConvert.SerializeObject(jsonGlossary, jsonSettings);

        if (overwritePrompt == true) {
            var minimalPrompt = new StringBuilder();
            minimalPrompt.AppendLine();
            minimalPrompt.AppendLine("Glossary:");
            minimalPrompt.AppendLine(jsonString);
            return minimalPrompt.ToString();
        }

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

public static class TbxStreamExtensions
{
    public static async Task<Stream> SanitizeTbxXmlAsync(this Stream original)
    {
        if (original == null)
            throw new ArgumentNullException(nameof(original));

        if (original.CanSeek)
            original.Position = 0;

        using var reader = new StreamReader(
            original,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        var text = await reader.ReadToEndAsync();

        text = text.TrimStart(
            '\uFEFF',
            '\u200B',
            '\u0000',
            '\u00A0',
            ' ', '\t', '\r', '\n');

        var bytes = Encoding.UTF8.GetBytes(text);
        var ms = new MemoryStream(bytes);
        ms.Position = 0;
        return ms;
    }
}
