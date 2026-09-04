using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal static class CaseListQuery
{
    public static IQueryable<Case> MatchingSearch(this IQueryable<Case> cases, string? search)
    {
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
        return filter switch
        {
            CaseStatusFilter.Open => cases.Where(static @case => @case.Status != CaseStatus.Closed),
            CaseStatusFilter.All => cases,
            CaseStatusFilter.Active => cases.Where(static @case => @case.Status == CaseStatus.Active),
            CaseStatusFilter.WaitingOnAuthority => cases.Where(static @case => @case.Status == CaseStatus.WaitingOnAuthority),
            CaseStatusFilter.Closed => cases.Where(static @case => @case.Status == CaseStatus.Closed),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown case status filter."),
        };
    }

    /// <summary>
    /// One level only; no read walks deeper (SDD-009).
    /// </summary>
    public static IQueryable<Case> WithParent(this IQueryable<Case> cases, Guid parentCaseId)
    {
        return cases.Where(@case => @case.ParentCaseId == parentCaseId);
    }

    public static IQueryable<Case> InListOrder(this IQueryable<Case> cases)
    {
        return cases
            .OrderByDescending(static @case => @case.Date)
            .ThenByDescending(static @case => @case.Created)
            .ThenByDescending(static @case => @case.Id);
    }

    public static IQueryable<Case> InChangeOrder(this IQueryable<Case> cases)
    {
        return cases
            .OrderByDescending(static @case => @case.Updated ?? @case.Created)
            .ThenByDescending(static @case => @case.Id);
    }

    public static IQueryable<CaseListItem> AsListItems(this IQueryable<Case> cases)
    {
        return cases.Select(static @case => new CaseListItem
        {
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
            Changed = @case.Updated ?? @case.Created,
        });
    }
}
