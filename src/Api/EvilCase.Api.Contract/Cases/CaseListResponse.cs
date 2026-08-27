namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListResponse
{
    public required IReadOnlyList<CaseListItem> Items { get; init; }

    public required CaseStatusCounts Counts { get; init; }
}
