using System.Xml.Linq;

namespace Tests.OpenAI.Ai;

public static class TestXliff
{
    private static readonly XNamespace Xliff = "urn:oasis:names:tc:xliff:document:2.0";

    public static string Untranslated(string sourceLanguage, string targetLanguage, IEnumerable<string> sources) =>
        Document(sourceLanguage, targetLanguage, sources.Select(source => Segment(source, null, null)));

    public static string Translated(string sourceLanguage, string targetLanguage, params (string Source, string Target)[] segments) =>
        Document(sourceLanguage, targetLanguage, segments.Select(segment => Segment(segment.Source, segment.Target, "translated")));

    private static (string Source, string? Target, string? State) Segment(string source, string? target, string? state) =>
        (source, target, state);

    private static string Document(
        string sourceLanguage,
        string targetLanguage,
        IEnumerable<(string Source, string? Target, string? State)> segments)
    {
        var units = segments.Select((segment, index) =>
        {
            var element = new XElement(Xliff + "segment", new XAttribute("id", $"s{index + 1}"));

            if (segment.State is not null)
            {
                element.Add(new XAttribute("state", segment.State));
            }

            element.Add(new XElement(Xliff + "source", segment.Source));

            if (segment.Target is not null)
            {
                element.Add(new XElement(Xliff + "target", segment.Target));
            }

            return new XElement(Xliff + "unit", new XAttribute("id", $"u{index + 1}"), element);
        });

        var root = new XElement(
            Xliff + "xliff",
            new XAttribute("version", "2.0"),
            new XAttribute("srcLang", sourceLanguage),
            new XAttribute("trgLang", targetLanguage),
            new XElement(Xliff + "file", new XAttribute("id", "f1"), new XAttribute("original", "test.xlf"), units));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }
}
