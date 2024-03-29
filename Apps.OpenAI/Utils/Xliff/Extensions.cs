using System.IO;
using System.Xml.Linq;

namespace Apps.OpenAI.Utils.Xliff;

public static class Extensions
{
    public static Stream ToStream(this XDocument xDocument)
    {
        var stream = new MemoryStream();
        xDocument.Save(stream);
        stream.Position = 0;
        return stream;
    }
}