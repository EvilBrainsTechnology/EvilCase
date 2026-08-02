namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// What narrows the case list. Both are absent by default, and absent means no narrowing at all.
/// </summary>
public sealed record CaseListRequest
{
    /// <summary>
    /// Matched against the title and the subject, case-insensitively, anywhere in either.
    /// </summary>
    public string? Search { get; init; }

    public CaseStatus? Status { get; init; }
}
