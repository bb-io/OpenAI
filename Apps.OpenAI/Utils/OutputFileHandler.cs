using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;

namespace Apps.OpenAI.Utils;

public static class OutputFileHandler
{
    public static async Task<FileReference> ToOutputFile(
        IFileManagementClient fileManagementClient, 
        Transformation content,
        string? outputFileHandling)
    {
        switch (outputFileHandling)
        {
            case "original":
            {
                var targetContentResult = content.Target();
                if (!targetContentResult.Success)
                    throw new PluginMisconfigurationException(targetContentResult.Error);
                var targetContent = targetContentResult.Value;
                return await fileManagementClient.UploadAsync(
                    targetContent.ToStream(), 
                    targetContent.OriginalMediaType, 
                    targetContent.OriginalName);
            }
            case "xliff1":
            {
                var xliff1String = Xliff1Serializer.Serialize(content);
                return await fileManagementClient.UploadAsync(
                    xliff1String.ToStream(), 
                    MediaTypes.Xliff1, 
                    content.BilingualFileName);
            }
            default:
                return await fileManagementClient.UploadAsync(
                    content.ToStream(), 
                    MediaTypes.Xliff2, 
                    content.BilingualFileName);
        }
    }
}