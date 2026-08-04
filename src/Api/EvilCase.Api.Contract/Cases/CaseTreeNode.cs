using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// A sub-case carrying its own sub-tree, to any depth.
/// </summary>
public sealed record CaseTreeNode
{
    public required long Id { get; init; }

    public required string CaseNumber { get; init; }

    public required string Title { get; init; }

    public required CaseStatus Status { get; init; }

    public required IReadOnlyList<CaseTreeNode> Children { get; init; }
}
