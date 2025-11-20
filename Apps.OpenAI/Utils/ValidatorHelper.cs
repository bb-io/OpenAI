using System.Linq;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.OpenAI.Utils;

public static class ValidatorHelper
{
    public static void ValidateInputFileContentType(FileReference file, params string[] allowedContentTypes)
    {
        string inputContentType = file.ContentType;
        if (!allowedContentTypes.Contains(inputContentType))
            throw new PluginMisconfigurationException($"Content type {inputContentType} is not supported for this action");
    }
}
