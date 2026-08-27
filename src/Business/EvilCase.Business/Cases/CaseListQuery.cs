using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Shapes the case list, one composable step per rule.
/// </summary>
public static class CaseListQuery
{
    public static IQueryable<Case> WithStatus(this IQueryable<Case> cases, CaseStatusFilter filter)
    {
        return filter switch
        {
            CaseStatusFilter.Open => cases.Where(@case => @case.Status != CaseStatus.Closed),
            CaseStatusFilter.All => cases,
            CaseStatusFilter.Active => cases.Where(@case => @case.Status == CaseStatus.Active),
            CaseStatusFilter.WaitingOnAuthority => cases.Where(@case => @case.Status == CaseStatus.WaitingOnAuthority),
            CaseStatusFilter.Closed => cases.Where(@case => @case.Status == CaseStatus.Closed),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown case status filter."),
        };
    }

    /// <summary>
    /// The direct subordinate cases of one case. The hierarchy is flat in the UI, so no read ever
    /// walks deeper (SDD-009).
    /// </summary>
    public static IQueryable<Case> WithParent(this IQueryable<Case> cases, Guid parentCaseId)
    {
        return cases.Where(@case => @case.ParentCaseId == parentCaseId);
    }

    /// <summary>
    /// Newest by the case's own date; equal dates fall back to when the row was written, and the
    /// UUIDv7 identifier makes the order total.
    /// </summary>
    public static IQueryable<Case> InListOrder(this IQueryable<Case> cases)
    {
        return cases
            .OrderByDescending(@case => @case.Date)
            .ThenByDescending(@case => @case.Created)
            .ThenByDescending(@case => @case.Id);
    }

    /// <summary>
    /// Reads only what a row shows, in one query.
    /// </summary>
    public static IQueryable<CaseListItem> AsListItems(this IQueryable<Case> cases)
    {
        return cases.Select(@case => new CaseListItem
        {
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
        });
    }
}
