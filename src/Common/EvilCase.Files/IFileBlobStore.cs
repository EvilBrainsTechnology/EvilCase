namespace EvilBrains.EvilCase.Files;

/// <summary>
/// A blob is never deleted: an orphaned one, from a failed write or a deleted row, stays on disk.
/// </summary>
public interface IFileBlobStore
{
    public Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken token);

    public Stream? ReadFileBlob(string storagePath);
}
