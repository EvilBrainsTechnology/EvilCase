namespace EvilBrains.EvilCase.Files;

/// <summary>
/// What a write measured: where the bytes landed, the checksum and the size the record stores.
/// </summary>
public sealed record FileBlobInfo
{
    /// <summary>
    /// Where the blob is, relative to the configured root, with forward slashes on every platform: the
    /// value is stored with the file asset, so a later layout scheme still finds this blob.
    /// </summary>
    public required string StoragePath { get; init; }

    /// <summary>
    /// SHA-256 of the content, lower-case hex.
    /// </summary>
    public required string ContentHash { get; init; }

    /// <summary>
    /// Size of the content in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }
}
