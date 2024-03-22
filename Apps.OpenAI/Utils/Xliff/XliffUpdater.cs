using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Apps.OpenAI.Utils.Xliff;

public class XliffUpdater
{
    private XDocument _xliffDoc;
    private XNamespace _ns = "urn:oasis:names:tc:xliff:document:1.2";

    public XliffUpdater(XDocument xliffDoc)
    {
        _xliffDoc = xliffDoc;
    }

    public void UpdateTranslationUnits(Dictionary<string, string> translatedUnits)
    {
        foreach (var unit in translatedUnits)
        {
            var transUnit = _xliffDoc.Descendants(_ns + "trans-unit")
                .FirstOrDefault(tu => (string)tu.Attribute("id") == unit.Key);

            if (transUnit != null)
            {
                var target = transUnit.Element(_ns + "target");
                if (target != null)
                {
                    // If the target element exists, update its value
                    target.Value = unit.Value;
                }
                else
                {
                    // Otherwise, create a new target element and add it to the trans-unit
                    target = new XElement(_ns + "target", unit.Value);
                    transUnit.Add(target);
                }
            }
        }
    }

    public XDocument GetUpdatedXliffDocument()
    {
        return _xliffDoc;
    }
}