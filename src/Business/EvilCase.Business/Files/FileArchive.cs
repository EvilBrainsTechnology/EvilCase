namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// The files of one act as one zip archive, its entries in the order they are written.
/// </summary>
public sealed record FileArchive
{
    public required Guid ActId { get; init; }

    public required string FileName { get; init; }

    public required IReadOnlyList<FileArchiveEntry> Entries { get; init; }
}
