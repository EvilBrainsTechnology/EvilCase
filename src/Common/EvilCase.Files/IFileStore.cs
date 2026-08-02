namespace EvilBrains.EvilCase.Files;

/// <summary>
/// Where a file asset's bytes live. Content-addressed: what identifies stored content is its SHA-256,
/// so storing the same bytes twice is one write and one copy.
/// </summary>
/// <remarks>
/// Nothing outside this namespace learns where the bytes actually are. Local disk today; object storage
/// would be a second implementation rather than a change to any caller.
/// </remarks>
public interface IFileStore
{
    /// <summary>
    /// Reads <paramref name="content"/> to its end, hashing as it goes, and keeps it under that hash.
    /// Content that is already stored is left alone rather than written again.
    /// </summary>
    public Task<StoredFile> Store(Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens stored content for reading, or returns null when nothing is stored under that hash. Missing
    /// content is an answer rather than an exception: an importer meets it whenever a database row
    /// outlives what it pointed at.
    /// </summary>
    public Task<Stream?> Open(string contentHash, CancellationToken cancellationToken = default);

    public Task<bool> Exists(string contentHash, CancellationToken cancellationToken = default);
}
