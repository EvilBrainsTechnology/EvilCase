namespace EvilBrains.EvilCase.Files;

/// <summary>
/// A blob is never deleted: an orphaned one, from a failed write, an unchanged upload or a deleted row, stays on disk.
/// </summary>
public interface IFileBlobStore
{
    public Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileVersionId, Stream content, CancellationToken token);

    public Stream? ReadFileBlob(string storagePath);
}
