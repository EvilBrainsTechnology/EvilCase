using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Lists;

/// <summary>
/// What every list of the application takes: the shared filter, the direction and the page.
/// </summary>
public abstract record ListRequest
{
    public string? Search { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public Guid? ContactId { get; init; }

    public IReadOnlyList<Guid> LabelIds { get; init; } = [];

    public ListSortDirection SortDirection { get; init; } = ListSortDirection.Descending;

    [Range(0, int.MaxValue)]
    public int Skip { get; init; }

    [Range(1, 100)]
    public required int Take { get; init; }
}
