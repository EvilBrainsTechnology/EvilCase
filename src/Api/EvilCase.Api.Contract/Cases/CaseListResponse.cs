namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListResponse
{
    public required IReadOnlyList<CaseListItem> Items { get; init; }

    /// <summary>
    /// Every row the filter leaves, whatever the page asks for.
    /// </summary>
    public required int TotalCount { get; init; }
}
