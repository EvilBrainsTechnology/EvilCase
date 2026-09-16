namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActListResponse
{
    public required IReadOnlyList<ActListItem> Items { get; init; }

    /// <summary>
    /// Every row the filter leaves, whatever the page asks for.
    /// </summary>
    public required int TotalCount { get; init; }
}
