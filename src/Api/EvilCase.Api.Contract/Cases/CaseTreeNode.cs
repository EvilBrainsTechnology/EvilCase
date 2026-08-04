using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// One sub-case, carrying the case it hangs under. The sub-tree travels flat: a nested one would inherit
/// whatever depth a JSON serializer allows, and a case nests to any depth.
/// </summary>
public sealed record CaseTreeNode
{
    public required long Id { get; init; }

    /// <summary>
    /// A top-level node names the case being read, which is not itself in <c>SubCases</c>.
    /// </summary>
    public required long ParentId { get; init; }

    public required string CaseNumber { get; init; }

    public required string Title { get; init; }

    public required CaseStatus Status { get; init; }
}
