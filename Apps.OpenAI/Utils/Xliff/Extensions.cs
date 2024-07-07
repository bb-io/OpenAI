using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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
        var tus = new List<TranslationUnit>();
        
        foreach (var tu in (from tu in xliffDocument.Descendants(defaultNs + "trans-unit") select tu).ToList())
        {
            tus.Add(new TranslationUnit
            {
                Source = RemoveExtraNewLines(Regex.Replace(tu.Element(defaultNs + "source").ToString(), @"</?source(.*?)>", @"")),
                Target = RemoveExtraNewLines(Regex.Replace(tu.Element(defaultNs + "target").ToString(), @"</?target(.*?)>", @"")),
                Id = tu.Attribute("id").Value                
            });
        }        

        return new ParsedXliff 
        {
            SourceLanguage = xliffDocument.Root?.Element(defaultNs + "file")?.Attribute("source-language")?.Value,
            TargetLanguage = xliffDocument.Root?.Element(defaultNs + "file")?.Attribute("target-language")?.Value,
            TranslationUnits = tus
        };
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
        fileStream.Position = 0;
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

}

public class ParsedXliff
{
    public string SourceLanguage { get; set; }
    public string TargetLanguage { get; set; }

    public List<TranslationUnit> TranslationUnits { get; set; }

}