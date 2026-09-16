using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Lists;

/// <summary>
/// What every list of the application takes: the page, the sort key and its direction.
/// </summary>
public abstract record ListRequest<TSortKey>
    where TSortKey : struct, Enum
{
    public virtual TSortKey Sort { get; init; }

    public virtual ListSortDirection SortDirection { get; init; } = ListSortDirection.Ascending;

    [Range(0, int.MaxValue)]
    public int Skip { get; init; }

    [Range(1, 100)]
    public required int Take { get; init; }
}
