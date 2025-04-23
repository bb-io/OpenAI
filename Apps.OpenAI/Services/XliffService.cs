using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Apps.OpenAI.Models.Entities;
using Apps.OpenAI.Services.Abstract;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Xliff.Utils;
using Blackbird.Xliff.Utils.Extensions;
using Blackbird.Xliff.Utils.Models;

namespace Apps.OpenAI.Services;

public class XliffService(IFileManagementClient fileManagementClient) : IXliffService
{
    public async Task<XliffDocument> LoadXliffDocumentAsync(FileReference file)
    {
        ValidateXliffFile(file);

        var stream = await fileManagementClient.DownloadAsync(file);
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        try
        {
            return memoryStream.ToXliffDocument();
        }
        catch(InvalidOperationException ex) when (ex.Message.Contains("Unsupported XLIFF version"))
        {
            throw new PluginMisconfigurationException("Unsupported XLIFF version. This action supports XLIFF 1.2, 2.1 and 2.2 versions only");
        }
    }

    public Stream SerializeXliffDocument(XliffDocument xliffDocument)
    {
        return xliffDocument.ToStream();
    }

    public IEnumerable<IEnumerable<TranslationUnit>> BatchTranslationUnits(
        IEnumerable<TranslationUnit> units, int batchSize)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        }
            
        return units
            .Select((unit, index) => new { Unit = unit, Index = index })
            .GroupBy(item => item.Index / batchSize)
            .Select(group => group.Select(item => item.Unit));
    }

    public bool HasUniqueTranslationIds(List<TranslationEntity> results)
    {
        var duplicates = results
            .GroupBy(r => r.TranslationId)
            .Where(g => g.Count() > 1)
            .ToList();

        return !duplicates.Any();
    }

    public Dictionary<string, string> CheckAndFixTagIssues(
        IEnumerable<TranslationUnit> units, Dictionary<string, string> translations, bool disableTagChecks)
    {
        if (disableTagChecks)
        {
            return translations;
        }
        
        return Utils.Xliff.Extensions.CheckTagIssues(units.ToList(), translations);
    }

    private static void ValidateXliffFile(FileReference file)
    {
        var acceptedFileExtensions = new[] { ".xlf", ".xliff", ".txlf", ".mqxliff", ".mxliff" };
        var fileExtension = Path.GetExtension(file.Name);
        if (string.IsNullOrEmpty(fileExtension) || !acceptedFileExtensions.Contains(fileExtension.ToLower()))
        {
            throw new PluginMisconfigurationException("Wrong format file. Please upload file format .xlf or .xliff.");
        }
    }
}