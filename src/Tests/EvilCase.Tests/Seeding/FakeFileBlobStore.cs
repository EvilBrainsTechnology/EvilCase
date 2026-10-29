using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Files;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeFileBlobStore : IFileBlobStore
{
    public Dictionary<Guid, string> Written { get; } = [];

    public Dictionary<string, string> WrittenByPath { get; } = [];

    public async Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileVersionId, Stream content, CancellationToken token)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        var text = await reader.ReadToEndAsync(token);

        this.Written[fileVersionId] = text;

        var storagePath = $"{tenantId}/{fileVersionId}";
        this.WrittenByPath[storagePath] = text;

        return new FileBlobInfo
        {
            StoragePath = storagePath,
            ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
            SizeBytes = Encoding.UTF8.GetByteCount(text),
        };
    }

    public Stream? ReadFileBlob(string storagePath)
    {
        return this.WrittenByPath.TryGetValue(storagePath, out var text) ? new MemoryStream(Encoding.UTF8.GetBytes(text)) : null;
    }
}
