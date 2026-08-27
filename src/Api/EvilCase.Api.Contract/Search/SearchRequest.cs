namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record SearchRequest
{
    /// <summary>
    /// Matched against titles, descriptions and every number. Under two characters nothing is searched.
    /// </summary>
    public string? Query { get; init; }
}
