namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record SearchResultItem
{
    public required SearchResultKind Kind { get; init; }

    public required Guid CaseId { get; init; }

    /// <summary>
    /// Set on an act, absent on a case; the two ids together say where the item navigates.
    /// </summary>
    public Guid? ActId { get; init; }

    public required string Number { get; init; }

    public required string Title { get; init; }

    public required DateOnly Date { get; init; }
}
