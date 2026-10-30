namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// A file version as the export reads it, before it has an entry in the archive.
/// </summary>
internal sealed record ExportedVersionRow
{
    public required Guid FileVersionId { get; init; }

    public required Guid FileId { get; init; }

    public required string FileName { get; init; }

    public required int Number { get; init; }

    public required long SizeBytes { get; init; }

    public required string? MediaType { get; init; }

    public required string ContentHash { get; init; }

    public required DateTime Created { get; init; }

    public required string UploaderEmail { get; init; }

    public required string StoragePath { get; init; }
}
