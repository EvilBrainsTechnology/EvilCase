namespace EvilBrains.EvilCase.Api.Contract.Lists;

public abstract record SortableListRequest<TSortKey> : ListRequest
    where TSortKey : struct, Enum
{
    public virtual TSortKey Sort { get; init; }

    public virtual ListSortDirection SortDirection { get; init; } = ListSortDirection.Ascending;
}
