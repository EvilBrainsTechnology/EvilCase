namespace EvilBrains.EvilCase.Import;

/// <summary>
/// What one folder tree would become. Nothing is written anywhere to produce it.
/// </summary>
public sealed record ImportedTree
{
    public required ImportedCase Root { get; init; }

    /// <summary>
    /// Everything the parser could not read, from the whole tree rather than per case.
    /// </summary>
    public IReadOnlyList<ImportProblem> Problems { get; init; } = [];
}
