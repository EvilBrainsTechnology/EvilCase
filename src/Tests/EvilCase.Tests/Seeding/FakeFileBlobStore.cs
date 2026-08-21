using System.Text;
using EvilBrains.EvilCase.Files;

namespace EvilBrains.EvilCase.Tests.Seeding;

internal sealed class FakeFileBlobStore : IFileBlobStore
{
    public Dictionary<Guid, string> Written { get; } = [];

    public async Task<FileBlobInfo> Write(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        var text = await reader.ReadToEndAsync(cancellationToken);

        this.Written[fileAssetId] = text;

        return new FileBlobInfo
        {
            StoragePath = $"{tenantId}/{fileAssetId}",
            ContentHash = new string('a', 64),
            SizeBytes = Encoding.UTF8.GetByteCount(text),
        };
    }

    public Task Delete(string storagePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
