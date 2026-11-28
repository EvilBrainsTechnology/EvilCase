namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// The files of one act or one case tree as one zip archive, its entries in the order they are written.
/// </summary>
public sealed record FileArchive
{
    /// <summary>
    /// The act or the case whose files the archive holds.
    /// </summary>
    public required Guid OwnerId { get; init; }

    public required string FileName { get; init; }

    public required IReadOnlyList<FileArchiveEntry> Entries { get; init; }
}
