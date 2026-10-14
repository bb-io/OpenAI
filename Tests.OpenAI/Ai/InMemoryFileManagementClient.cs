using System.Text;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.OpenAI.Ai;

public sealed class InMemoryFileManagementClient : IFileManagementClient
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public FileReference Add(string name, string content, string? contentType = null)
    {
        _files[name] = Encoding.UTF8.GetBytes(content);
        return new FileReference { Name = name, ContentType = contentType! };
    }

    public string Read(FileReference file) => Encoding.UTF8.GetString(_files[file.Name]);

    public Task<Stream> DownloadAsync(FileReference reference) =>
        Task.FromResult<Stream>(new MemoryStream(_files[reference.Name], writable: false));

    public async Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        _files[fileName] = buffer.ToArray();
        return new FileReference { Name = fileName, ContentType = contentType };
    }
}
