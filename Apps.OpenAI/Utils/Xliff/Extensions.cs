using System.IO;
using System.Text;
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
}