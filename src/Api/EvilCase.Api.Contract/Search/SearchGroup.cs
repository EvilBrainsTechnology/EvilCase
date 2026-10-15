namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record SearchGroup<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    /// <summary>
    /// Everything the term matches, however many rows <see cref="Items"/> carries.
    /// </summary>
    public required int TotalCount { get; init; }
}
