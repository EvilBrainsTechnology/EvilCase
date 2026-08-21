using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Shapes the case list, one composable step per rule.
/// </summary>
public static class CaseListQuery
{
    /// <summary>
    /// Newest by the case's own date; equal dates fall back to when the row was written, and the
    /// UUIDv7 identifier makes the order total.
    /// </summary>
    public static IQueryable<Case> InListOrder(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

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
        ArgumentNullException.ThrowIfNull(cases);

        return cases.Select(@case => new CaseListItem
        {
            Id = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
        });
    }
}
