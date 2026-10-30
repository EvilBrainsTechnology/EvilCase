namespace EvilBrains.EvilCase.Api.Contract.ApiTokens;

public sealed record ApiTokenListResponse
{
    public required IReadOnlyList<ApiTokenItem> Items { get; init; }
}
