using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListRequest
{
    public string? Search { get; init; }

    public CaseStatusFilter Status { get; init; } = CaseStatusFilter.Open;

    public CaseListOrder Order { get; init; } = CaseListOrder.Date;

    [Range(1, 100)]
    public int? Take { get; init; }
}
