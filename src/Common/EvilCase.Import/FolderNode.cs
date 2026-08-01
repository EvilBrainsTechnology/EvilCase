namespace EvilBrains.EvilCase.Import;

/// <summary>
/// A folder tree as data. The parser takes one of these rather than a path, so it is testable without a
/// filesystem and cannot write to the source it is reading.
/// </summary>
public sealed record FolderNode
{
    public required string Name { get; init; }

    public IReadOnlyList<string> Files { get; init; } = [];

    public IReadOnlyList<FolderNode> Folders { get; init; } = [];
}
