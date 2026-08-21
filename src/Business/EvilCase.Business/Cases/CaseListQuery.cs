using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Shapes the case list, one composable step per rule.
/// </summary>
public static class CaseListQuery
{
    /// <summary>
    /// Matches the title or the description, ignoring case and diacritics. A blank term narrows nothing.
    /// </summary>
    public static IQueryable<Case> MatchingSearch(this IQueryable<Case> cases, string? search)
    {
        ArgumentNullException.ThrowIfNull(cases);

        if (string.IsNullOrWhiteSpace(search))
            return cases;

        var pattern = $"%{search.Trim().EscapeLikeWildcards()}%";

        return cases.Where(@case =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(@case.Title), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || (@case.Description != null
                    && EF.Functions.ILike(DatabaseFunctions.Unaccent(@case.Description), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)));
    }

    public static IQueryable<Case> WithStatus(this IQueryable<Case> cases, CaseStatusFilter filter)
    {
        ArgumentNullException.ThrowIfNull(cases);

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
    /// Most recently touched first, the identifier breaking the tie so the order is total.
    /// </summary>
    public static IQueryable<Case> InListOrder(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases
            .OrderByDescending(@case => @case.Updated ?? @case.Created)
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
            Title = @case.Title,
            Description = @case.Description,
            Status = @case.Status,
            Created = @case.Created,
            Updated = @case.Updated,
        });
    }
}
