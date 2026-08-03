namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// What narrows the case list.
/// </summary>
public sealed record CaseListRequest
{
    /// <summary>
    /// Matched against the title and the subject, case-insensitively, anywhere in either.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Defaults to <see cref="CaseStatusFilter.Open"/> — a request that says nothing gets the open
    /// cases, not every case (#100).
    /// </summary>
    public CaseStatusFilter Status { get; init; } = CaseStatusFilter.Open;
}
