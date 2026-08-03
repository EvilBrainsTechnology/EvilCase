namespace EvilBrains.EvilCase.Files;

/// <summary>
/// Where a file asset's bytes live, content-addressed by SHA-256.
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// Reads <paramref name="content"/> to its end, hashing as it goes, and keeps it under that hash.
    /// Content that is already stored is left alone rather than written again.
    /// </summary>
    public Task<StoredFile> Store(Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens stored content, or null when nothing is stored under that hash.
    /// </summary>
    public Task<Stream?> Open(string contentHash, CancellationToken cancellationToken = default);

    public Task<bool> Exists(string contentHash, CancellationToken cancellationToken = default);
}
