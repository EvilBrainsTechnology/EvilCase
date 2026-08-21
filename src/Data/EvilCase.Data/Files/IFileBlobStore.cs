namespace EvilBrains.EvilCase.Data.Files;

/// <summary>
/// The bytes go to {root}/{tenantId}/{fileAssetId}; the store knows no name and no media type,
/// those are the record's. The write runs before the database commit, so a blob orphaned by a
/// failed transaction is tolerated and never cleaned up.
/// </summary>
public interface IFileBlobStore
{
    /// <summary>
    /// Writes the content and returns the checksum and size to put on the record.
    /// </summary>
    public Task<FileBlobInfo> Write(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the blob. Returns <see langword="true"/> where a blob was there, <see langword="false"/>
    /// where it was already gone — a record whose blob vanished still has to be deletable.
    /// </summary>
    public bool Delete(Guid tenantId, Guid fileAssetId);
}
