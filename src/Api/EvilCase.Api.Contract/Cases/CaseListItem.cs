using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListItem
{
    public required Guid CaseId { get; init; }

    public required string CaseNumber { get; init; }

    public required string Title { get; init; }

    public required DateOnly Date { get; init; }

    public required CaseStatus Status { get; init; }

    /// <summary>
    /// When the case itself last changed: its Updated, or its Created while it has never been
    /// edited. An act, a comment or a file of the case never moves it (SDD-015).
    /// </summary>
    public required DateTime Changed { get; init; }
}
