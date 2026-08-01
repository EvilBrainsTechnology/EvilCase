namespace EvilBrains.EvilCase.Import;

/// <summary>
/// One folder read as a case. A sub-folder is a sub-case with the same shape, to any depth.
/// </summary>
public sealed record ImportedCase
{
    /// <summary>
    /// The folder name, without the closed suffix where it carried one.
    /// </summary>
    public required string Title { get; init; }

    public required bool IsClosed { get; init; }

    public IReadOnlyList<ImportedAct> Acts { get; init; } = [];

    public IReadOnlyList<ImportedCase> SubCases { get; init; } = [];
}
