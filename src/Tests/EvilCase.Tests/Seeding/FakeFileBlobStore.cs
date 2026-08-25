using System.Text;
using EvilBrains.EvilCase.Files;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeFileBlobStore : IFileBlobStore
{
    public Dictionary<Guid, string> Written { get; } = [];

    public List<string> Deleted { get; } = [];

    public async Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken token)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        var text = await reader.ReadToEndAsync(token);

        this.Written[fileAssetId] = text;

        return new FileBlobInfo
        {
            StoragePath = $"{tenantId}/{fileAssetId}",
            ContentHash = new string('a', 64),
            SizeBytes = Encoding.UTF8.GetByteCount(text),
        };
    }

    public Task DeleteFileBlob(string storagePath, CancellationToken token)
    {
        this.Deleted.Add(storagePath);

        return Task.CompletedTask;
    }
}
