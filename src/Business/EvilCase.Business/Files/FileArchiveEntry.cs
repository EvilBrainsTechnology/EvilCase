namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// One file version under the name it carries in the archive.
/// </summary>
public sealed record FileArchiveEntry
{
    public required string EntryName { get; init; }

    /// <summary>
    /// The moment the version was uploaded, in UTC.
    /// </summary>
    public required DateTime Created { get; init; }

    public required string StoragePath { get; init; }
}
