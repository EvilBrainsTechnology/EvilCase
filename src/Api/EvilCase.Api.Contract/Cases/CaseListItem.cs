using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListItem
{
    public required Guid CaseId { get; init; }

    public required string CaseNumber { get; init; }

    public required string Title { get; init; }

    public required DateOnly Date { get; init; }

    public required CaseStatus Status { get; init; }
}
