namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// A case with its sub-cases, in the order of the case's sub-case list, and its acts, in the order of its act list.
/// </summary>
public sealed record CaseFileTree
{
    public required CaseListItem Case { get; init; }

    public IReadOnlyList<CaseFileTree> SubCases { get; init; } = [];

    public IReadOnlyList<ActFileTree> Acts { get; init; } = [];
}
