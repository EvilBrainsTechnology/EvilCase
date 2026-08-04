using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseDetailResponse
{
    public required long Id { get; init; }

    public required string CaseNumber { get; init; }

    public required string Title { get; init; }

    public string? Subject { get; init; }

    public required CaseStatus Status { get; init; }

    public required IReadOnlyList<string> Tags { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    /// <summary>
    /// Root first, empty on a root case.
    /// </summary>
    public IReadOnlyList<CaseAncestor> Ancestors { get; init; } = [];

    /// <summary>
    /// Direct sub-cases, each carrying its own sub-tree.
    /// </summary>
    public IReadOnlyList<CaseTreeNode> SubCases { get; init; } = [];

    /// <summary>
    /// Newest first.
    /// </summary>
    public IReadOnlyList<CaseComment> Comments { get; init; } = [];
}
