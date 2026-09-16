using EvilBrains.EvilCase.Api.Contract.Lists;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListRequest : ListRequest
{
    public CaseStatusFilter Status { get; init; } = CaseStatusFilter.Open;

    /// <summary>
    /// Ignored where <see cref="ParentCaseId"/> is given.
    /// </summary>
    public CaseListScope Scope { get; init; } = CaseListScope.RootOnly;

    public Guid? ParentCaseId { get; init; }

    public CaseSortKey Sort { get; init; } = CaseSortKey.Date;
}
