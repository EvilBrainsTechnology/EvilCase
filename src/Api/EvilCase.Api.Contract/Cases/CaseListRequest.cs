namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListRequest
{
    public CaseStatusFilter Status { get; init; } = CaseStatusFilter.Open;
}
