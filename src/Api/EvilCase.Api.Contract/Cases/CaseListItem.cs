namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// One row of the case list. Carries what a row shows and nothing the detail page owns — no parties, no
/// acts, no sub-case tree, only how many sub-cases hang under it.
/// </summary>
public sealed record CaseListItem
{
    public required long Id { get; init; }

    public required string Title { get; init; }

    public string? Subject { get; init; }

    public required CaseStatus Status { get; init; }

    public required IReadOnlyList<string> Tags { get; init; }

    public required DateTime Created { get; init; }

    /// <summary>
    /// Null until the case is first changed.
    /// </summary>
    public DateTime? Updated { get; init; }

    /// <summary>
    /// Direct children only. The list is roots-only, so this is what tells a row that there is a tree
    /// behind it.
    /// </summary>
    public required int SubCaseCount { get; init; }
}
