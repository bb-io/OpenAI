using System.Linq;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Extensions;

public static class FileReferenceExtensions
{
    public static bool IsAudio(this FileReference fileReference)
    {
        return 
            fileReference.ContentType.StartsWith("audio") || 
            fileReference.Name.EndsWith(".wav") || 
            fileReference.Name.EndsWith(".mp3");
    }

    public static bool IsImage(this FileReference fileReference)
    {
        return
            fileReference.ContentType.StartsWith("image") || 
            new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" }.Any(fileReference.Name.EndsWith);
    }
}