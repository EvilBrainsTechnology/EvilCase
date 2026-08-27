namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record SearchResponse
{
    public required IReadOnlyList<SearchResultItem> Items { get; init; }

    /// <summary>
    /// The entity Enter opens, absent where the term is no entity's number.
    /// </summary>
    public SearchResultItem? ExactMatch { get; init; }
}
