using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.OpenAI.Base;

public class FileManager : IFileManagementClient
{
    private readonly string _folderLocation;
    
    public FileManager(string folderLocation)
    {
        _folderLocation = folderLocation ?? throw new ArgumentNullException(nameof(folderLocation));
    }
    
    public async Task<Stream> DownloadAsync(FileReference reference)
    {
        if (reference == null) throw new ArgumentNullException(nameof(reference));
        
        var path = Path.Combine(_folderLocation, "Input", reference.Name);
        var bytes = await File.ReadAllBytesAsync(path);
        
        return new MemoryStream(bytes);
    }
    
    public async Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
        
        var path = Path.Combine(_folderLocation, "Output", fileName);
        var directory = Path.GetDirectoryName(path);
        
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
            
        using (var fileStream = File.Create(path))
        {
            await stream.CopyToAsync(fileStream);
        }
        
        return new FileReference { Name = fileName };
    }
}
