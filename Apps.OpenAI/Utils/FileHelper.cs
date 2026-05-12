using System;

namespace Apps.OpenAI.Utils;

public static class FileHelper
{
    public static string GenerateBase64String(string mimeType, byte[] fileBytes)
    {
        return $"data:{mimeType};base64,{Convert.ToBase64String(fileBytes)}";
    }
}