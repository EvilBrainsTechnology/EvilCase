namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListItem
{
    public required long Id { get; init; }

    public required string Title { get; init; }

    public string? Subject { get; init; }

    public required CaseStatus Status { get; init; }

    public required IReadOnlyList<string> Tags { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    /// <summary>
    /// Direct children only.
    /// </summary>
    public required int SubCaseCount { get; init; }
}
