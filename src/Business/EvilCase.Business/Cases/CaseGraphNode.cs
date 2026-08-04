using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// One case as the database returns it for a tree walk: flat, carrying the parent it hangs under.
/// Nesting is the one shape SQL cannot return, so it is built from these.
/// </summary>
public sealed record CaseGraphNode
{
    public required long Id { get; init; }

    public required long? ParentCaseId { get; init; }

    public required string CaseNumber { get; init; }

    public required string Title { get; init; }

    public required CaseStatus Status { get; init; }
}
