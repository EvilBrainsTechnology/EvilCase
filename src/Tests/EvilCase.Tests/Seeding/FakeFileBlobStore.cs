using System.Text;
using EvilBrains.EvilCase.Files;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeFileBlobStore : IFileBlobStore
{
    public Dictionary<Guid, string> Written { get; } = [];

    public Dictionary<string, string> WrittenByPath { get; } = [];

    public List<string> Deleted { get; } = [];

    public async Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken token)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        var text = await reader.ReadToEndAsync(token);

        this.Written[fileAssetId] = text;

        var storagePath = $"{tenantId}/{fileAssetId}";
        this.WrittenByPath[storagePath] = text;

        return new FileBlobInfo
        {
            StoragePath = storagePath,
            ContentHash = new string('a', 64),
            SizeBytes = Encoding.UTF8.GetByteCount(text),
        };
    }

    public Stream? ReadFileBlob(string storagePath)
    {
        return this.WrittenByPath.TryGetValue(storagePath, out var text) ? new MemoryStream(Encoding.UTF8.GetBytes(text)) : null;
    }

    public Task DeleteFileBlob(string storagePath, CancellationToken token)
    {
        this.Deleted.Add(storagePath);

        return Task.CompletedTask;
    }
}
