namespace EvilBrains.EvilCase.Files;

public sealed record FileBlobInfo
{
    /// <summary>
    /// Relative, forward slashes on every platform; persisted, so a layout change must still read it.
    /// </summary>
    public required string StoragePath { get; init; }

    public required string ContentHash { get; init; }

    public required long SizeBytes { get; init; }
}
