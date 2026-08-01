namespace EvilBrains.EvilCase.Import;

/// <summary>
/// A name the parser could not read. Reported rather than skipped: a file that silently vanishes from
/// an import is worse than one the preview shows as unreadable.
/// </summary>
public sealed record ImportProblem
{
    /// <summary>
    /// Folder names from the root down, joined by <c>/</c>, ending in the file name.
    /// </summary>
    public required string Path { get; init; }

    public required string Reason { get; init; }
}
