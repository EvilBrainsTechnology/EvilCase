namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActListResponse
{
    public required IReadOnlyList<ActListItem> Items { get; init; }

    public required int TotalCount { get; init; }
}
