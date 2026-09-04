namespace EvilBrains.EvilCase.Files;

/// <summary>
/// Written before the database commit: an orphaned blob stays and nothing cleans it up.
/// </summary>
public interface IFileBlobStore
{
    public Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken token);

    public Stream? ReadFileBlob(string storagePath);

    public Task DeleteFileBlob(string storagePath, CancellationToken token);
}
