namespace EvilBrains.EvilCase.Business.Export;

internal sealed record ExportedFile
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; }

    /// <summary>
    /// By number; the last is the current version.
    /// </summary>
    public required IReadOnlyList<ExportedFileVersion> Versions { get; init; }
}
