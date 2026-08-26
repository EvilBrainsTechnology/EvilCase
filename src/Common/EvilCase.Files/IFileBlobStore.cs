namespace EvilBrains.EvilCase.Files;

/// <summary>
/// The bytes go to {root}/{tenantId}/{aa}/{bb}/{fileAssetId}; the store knows no name and no media type,
/// those are the record's. The write runs before the database commit, so a blob orphaned by a failed
/// transaction is tolerated and never cleaned up.
/// </summary>
public interface IFileBlobStore
{
    /// <summary>
    /// Writes the bytes and returns where they landed. The write is atomic: a temporary file, then a rename.
    /// </summary>
    public Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken token);

    /// <summary>
    /// Opens the blob at the stored path for reading, or null where no blob is there. The caller owns
    /// the stream.
    /// </summary>
    public Stream? ReadFileBlob(string storagePath);

    /// <summary>
    /// Removes the blob at the stored path. A path with no blob behind it is not an error.
    /// </summary>
    public Task DeleteFileBlob(string storagePath, CancellationToken token);
}
