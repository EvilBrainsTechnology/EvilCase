using EvilBrains.EvilCase.Api.Contract.Lists;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListRequest : ListRequest<CaseSortKey>
{
    public override CaseSortKey Sort { get; init; } = CaseSortKey.Date;

    public override ListSortDirection SortDirection { get; init; } = ListSortDirection.Descending;

    public string? Search { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public Guid? ContactId { get; init; }

    public IReadOnlyList<Guid> LabelIds { get; init; } = [];

    public CaseStatusFilter Status { get; init; } = CaseStatusFilter.Open;

    /// <summary>
    /// Ignored where <see cref="ParentCaseId"/> is given.
    /// </summary>
    public CaseListScope Scope { get; init; } = CaseListScope.RootOnly;

    public Guid? ParentCaseId { get; init; }
}
