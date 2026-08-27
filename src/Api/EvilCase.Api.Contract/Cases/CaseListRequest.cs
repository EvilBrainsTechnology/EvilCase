using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListRequest
{
    /// <summary>
    /// Matched against the title and the description.
    /// </summary>
    public string? Search { get; init; }

    public CaseStatusFilter Status { get; init; } = CaseStatusFilter.Open;

    public CaseListOrder Order { get; init; } = CaseListOrder.Date;

    /// <summary>
    /// The most rows a page returns; the whole list when absent.
    /// </summary>
    [Range(1, 100)]
    public int? Take { get; init; }
}
