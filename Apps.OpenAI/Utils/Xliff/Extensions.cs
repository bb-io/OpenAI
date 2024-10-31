using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Blackbird.Xliff.Utils.Models;
using static Apps.OpenAI.Utils.Xliff.ParsedXliff;

namespace Apps.OpenAI.Utils.Xliff;

public static class Extensions
{
    public static Stream ToStream(this XDocument xDoc)
    {
        var stringWriter = new StringWriter();
        xDoc.Save(stringWriter);
        var encoding = stringWriter.Encoding;

        var content = stringWriter.ToString();
        content = content.Replace("&lt;", "<").Replace("&gt;", ">");
        content = content.Replace(" />", "/>");

        var memoryStream = new MemoryStream(encoding.GetBytes(content));
        memoryStream.Position = 0;

        return memoryStream;
    }
    public static ParsedXliff ParseXLIFF(Stream file)
    {
        var xliffDocument = XDocument.Load(file);
        XNamespace defaultNs = xliffDocument.Root.GetDefaultNamespace();
        var tus = new List<TransUnit>();
        
        foreach (var tu in (from tu in xliffDocument.Descendants(defaultNs + "trans-unit") select tu).ToList())
        {
            string src = "";
            if (tu.Elements().Any(x => x.Name == defaultNs + "seg-source"))
            { src = RemoveExtraNewLines(Regex.Replace(tu.Element(defaultNs + "seg-source").ToString(), @"</?seg-source(.*?)>", @"")); }
            else src = RemoveExtraNewLines(Regex.Replace(tu.Element(defaultNs + "source").ToString(), @"</?source(.*?)>", @""));
            tus.Add(new TransUnit
            {
                Source = src,
                Target = RemoveExtraNewLines(Regex.Replace(tu.Element(defaultNs + "target").ToString(), @"</?target(.*?)>", @"")),
                Id = tu.Attribute("id").Value,
                Tags = GetTags(src)
            });
        }        

        return new ParsedXliff 
        {
            SourceLanguage = xliffDocument.Root?.Element(defaultNs + "file")?.Attribute("source-language")?.Value,
            TargetLanguage = xliffDocument.Root?.Element(defaultNs + "file")?.Attribute("target-language")?.Value,
            TranslationUnits = tus
        };
    }

    private static List<Blackbird.Xliff.Utils.Models.Tag> GetTags(string src)
    {
        var parsedTags = new List<Blackbird.Xliff.Utils.Models.Tag>();
    
        var tagPattern = @"<(ept|bpt|ph|g)(\s+[^>]*)?>(.*?)</\1>";

        var tags = Regex.Matches(src, tagPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (tags == null || tags.Count == 0) return parsedTags;
        var count = 0;
        foreach (Match match in tags)
        {
            count++;

            var tagName = match.Groups[1].Value.ToLower(); 
            var attributes = match.Groups[2].Value;        
            var fullTag = match.Value;                     

            var idMatch = Regex.Match(attributes, @"id=""(.*?)""", RegexOptions.IgnoreCase);
            var id = idMatch.Success ? idMatch.Groups[1].Value : string.Empty;

            parsedTags.Add(new Blackbird.Xliff.Utils.Models.Tag
            {
                Position = count,
                Id = id,
                Value = fullTag,
                Type = tagName
            });
        }
    
        return parsedTags;
    }

    public static string RemoveExtraNewLines(string originalString)
    {
        if (!string.IsNullOrWhiteSpace(originalString))
        {
            var to_modify = originalString;
            to_modify = Regex.Replace(to_modify, @"\r\n(\s+)?", "", RegexOptions.Multiline);
            return to_modify;
        }
        else
        {
            return string.Empty;
        }
    }

    public static Stream UpdateOriginalFile(Stream fileStream, Dictionary<string, string> results)
    {
        string fileContent;
        Encoding encoding;
        
        using (StreamReader inFileStream = new StreamReader(fileStream))
        {
            encoding = inFileStream.CurrentEncoding;
            fileContent = inFileStream.ReadToEnd();
        }

        var tus = Regex.Matches(fileContent, @"<trans-unit [\s\S]+?</trans-unit>").Select(x => x.Value);
        foreach (var tu in tus) 
        { 
            var id = Regex.Match(tu, @"trans-unit id=""(.*?)""").Groups[1].Value;
            if (results.ContainsKey(id)) 
            {
                var newtu = Regex.Replace(tu, "(<target(.*?)>)([\\s\\S]+?)(</target>)", "${1}" + results[id] +"${4}");
                fileContent = Regex.Replace(fileContent, Regex.Escape(tu), newtu);

            } 
            else continue;
        }
        return new MemoryStream(encoding.GetBytes(fileContent));
    }
    public static Dictionary<string, string> CheckTagIssues(List<TranslationUnit> translationUnits, Dictionary<string, string> results)
    {
        var changesToImplement = new Dictionary<string, string>();
        foreach (var update in results)
        {
            var newTags = GetTags(update.Value);
            if (AreTagsOk(translationUnits.FirstOrDefault(x => x.Id == update.Key).Tags, newTags)) 
            {
                changesToImplement.Add(update.Key, update.Value);
            }
        }

        return changesToImplement;
    }

    private static bool AreTagsOk(List<Blackbird.Xliff.Utils.Models.Tag> tags, List<Blackbird.Xliff.Utils.Models.Tag> newTags)
    {
        var filteredTags = tags.Where(t => t.Type == "bpt" || t.Type == "ept").ToList();
        var filteredNewTags = newTags.Where(t => t.Type == "bpt" || t.Type == "ept").ToList();

        if (filteredTags.Count != filteredNewTags.Count)
        {
            return false;
        }

        foreach (var tag in filteredNewTags)
        {
            if (filteredTags.Any(x => x.Id == tag.Id && x.Type == tag.Type && x.Value == tag.Value && x.Position == tag.Position))
            {
                continue;
            }
            else if (filteredTags.Any(x => x.Id == tag.Id && x.Type == tag.Type && x.Value == tag.Value))
            {
                if (tag.Type == "bpt")
                {
                    if (filteredNewTags.Any(x => x.Type == "ept" && x.Id == tag.Id && x.Position > tag.Position))
                        continue;
                    else
                        return false;
                }
                else if (tag.Type == "ept")
                {
                    if (filteredNewTags.Any(x => x.Type == "bpt" && x.Id == tag.Id && x.Position < tag.Position))
                        continue;
                    else
                        return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}

