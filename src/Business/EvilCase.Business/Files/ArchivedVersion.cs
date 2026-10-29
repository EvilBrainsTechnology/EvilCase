namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// A file version as the archive reads it, before it has an entry name.
/// </summary>
internal sealed record ArchivedVersion
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; }

    public required int Number { get; init; }

    public required DateTime Created { get; init; }

    public required string StoragePath { get; init; }
}
