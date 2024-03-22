using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Apps.OpenAI.Utils.Xliff;

public class XliffExtractor
{
    private readonly XDocument _xliffDoc;
    private readonly XNamespace _ns = "urn:oasis:names:tc:xliff:document:1.2";

    public XliffExtractor(XDocument xDocument)
    {
        _xliffDoc = xDocument;
    }

    public string ExtractSourceLanguage()
    {
        var fileElement = GetFileElement();
        return (string)fileElement.Attribute("source-language");
    }

    public string ExtractTargetLanguage()
    {
        var fileElement = GetFileElement();
        return (string)fileElement.Attribute("target-language");
    }

    public Dictionary<string, string> ExtractTranslationUnits()
    {
        var fileElement = GetFileElement();
        var translationUnits = fileElement
            .Element(_ns + "body")?
            .Elements(_ns + "trans-unit")
            .ToDictionary(
                tu => (string)tu.Attribute("id"),
                tu => (string)tu.Element(_ns + "source")
            );

        return translationUnits ?? new Dictionary<string, string>();
    }

    private XElement GetFileElement()
    {
        var fileElement = _xliffDoc.Element(_ns + "xliff")?.Element(_ns + "file");
        if (fileElement == null)
        {
            throw new InvalidOperationException("XLIFF file element is missing.");
        }
        return fileElement;
    }
}