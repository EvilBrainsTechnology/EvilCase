using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Lists;

/// <summary>
/// What every list of the application takes: the page and the sort direction.
/// </summary>
public abstract record ListRequest
{
    public ListSortDirection SortDirection { get; init; } = ListSortDirection.Descending;

    [Range(0, int.MaxValue)]
    public int Skip { get; init; }

    [Range(1, 100)]
    public required int Take { get; init; }
}
