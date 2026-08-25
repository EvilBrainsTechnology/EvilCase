using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseDetail
{
    public required Guid Id { get; init; }

    public required string CaseNumber { get; init; }

    public required DateOnly Date { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required CaseStatus Status { get; init; }

    /// <summary>
    /// The case this one hangs under, or null where it hangs under nothing (SDD-009).
    /// </summary>
    public CaseListItem? ParentCase { get; init; }

    /// <summary>
    /// The cases that hang directly under this one; the detail shows one level, never a tree (SDD-009).
    /// </summary>
    public IReadOnlyList<CaseListItem> ChildCases { get; init; } = [];
}
