namespace EvilBrains.EvilCase.Files;

/// <summary>
/// The bytes go to {root}/{tenantId}/{aa}/{bb}/{fileAssetId}{extension}; the store knows no name and no
/// media type beyond the extension it keeps, those are the record's. The write runs before the database
/// commit, so a blob orphaned by a failed transaction is tolerated and never cleaned up.
/// </summary>
public interface IFileBlobStore
{
    /// <summary>
    /// Writes the bytes and returns where they landed. The write is atomic: a temporary file, then a rename.
    /// <paramref name="fileName"/> is trusted for nothing but a safe extension on the stored blob name.
    /// </summary>
    public Task<FileBlobInfo> Write(Guid tenantId, Guid fileAssetId, string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the blob at the stored path. A path with no blob behind it is not an error.
    /// </summary>
    public Task Delete(string storagePath, CancellationToken cancellationToken = default);
}
