using System;
using System.IO;
using System.Linq;
using Apps.OpenAI.Constants;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.OpenAI.Extensions;

public static class FileReferenceExtensions
{
    public static bool IsAudio(this FileReference fileReference)
    {
        return 
            (!string.IsNullOrWhiteSpace(fileReference.ContentType) && fileReference.ContentType.StartsWith("audio")) || 
            fileReference.Name.EndsWith(".wav") || 
            fileReference.Name.EndsWith(".mp3");
    }

    public static bool IsImage(this FileReference fileReference)
    {
        return
            (!string.IsNullOrWhiteSpace(fileReference.ContentType) && fileReference.ContentType.StartsWith("image")) || 
            new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" }.Any(fileReference.Name.EndsWith);
    }

    public static bool IsSupportedFileType(this FileReference fileReference)
    {
        bool isMimeSupported = !string.IsNullOrWhiteSpace(fileReference.ContentType) && 
                               FileConstants.SupportedMimeTypes.Contains(fileReference.ContentType);

        string fileExtension = Path.GetExtension(fileReference.Name);
        bool isExtensionSupported =
            FileConstants.SupportedFileExtensions.Any(ext => fileExtension.Equals(ext, StringComparison.OrdinalIgnoreCase));

        return isMimeSupported || isExtensionSupported;
    }
}