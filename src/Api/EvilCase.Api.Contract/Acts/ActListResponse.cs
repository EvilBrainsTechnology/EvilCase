namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActListResponse
{
    public required IReadOnlyList<ActListItem> Items { get; init; }
}
