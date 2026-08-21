namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListRequest
{
    /// <summary>
    /// Matched against the title and the description.
    /// </summary>
    public string? Search { get; init; }

    public CaseStatusFilter Status { get; init; } = CaseStatusFilter.Open;
}
