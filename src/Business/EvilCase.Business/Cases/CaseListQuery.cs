using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Entities;
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

        var pattern = search.ContainsLikePattern();

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
    /// A named parent takes precedence over the scope and leaves its direct children only; no read
    /// walks deeper (SDD-009).
    /// </summary>
    public static IQueryable<Case> UnderParent(this IQueryable<Case> cases, Guid? parentCaseId, CaseListScope scope)
    {
        if (parentCaseId is not null)
            return cases.Where(@case => @case.ParentCaseId == parentCaseId);

        return scope switch
        {
            CaseListScope.RootOnly => cases.Where(static @case => @case.ParentCaseId == null),
            CaseListScope.All => cases,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown case list scope."),
        };
    }

    public static IQueryable<Case> WithContact(this IQueryable<Case> cases, Guid? contactId)
    {
        return contactId is null ? cases : cases.Where(@case => @case.ContactId == contactId);
    }

    public static IQueryable<Case> WithLabels(this IQueryable<Case> cases, IReadOnlyList<Guid> labelIds)
    {
        foreach (var labelId in labelIds)
            cases = cases.Where(@case => @case.Labels.Any(assignment => assignment.LabelId == labelId));

        return cases;
    }

    public static IQueryable<Case> WithinDates(this IQueryable<Case> cases, DateOnly? from, DateOnly? to)
    {
        if (from is not null)
            cases = cases.Where(@case => @case.Date >= from);

        if (to is not null)
            cases = cases.Where(@case => @case.Date <= to);

        return cases;
    }

    public static IQueryable<Case> InSortOrder(this IQueryable<Case> cases, CaseSortKey sort, ListSortDirection direction)
    {
        return sort switch
        {
            CaseSortKey.Date => cases.InKeyOrder(static @case => @case.Date, direction).ThenInWriteOrder(direction),
            CaseSortKey.Changed => cases.InKeyOrder(static @case => @case.Updated ?? @case.Created, direction).ThenInWriteOrder(direction),
            CaseSortKey.Title => cases.InKeyOrder(static @case => @case.Title, direction).ThenInWriteOrder(direction),
            CaseSortKey.CaseNumber => cases.InKeyOrder(static @case => @case.CaseNumber, direction).ThenInWriteOrder(direction),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unknown case sort key."),
        };
    }

    public static IQueryable<CaseListItem> AsListItems(this IQueryable<Case> cases)
    {
        return cases.Select(static @case => new CaseListItem
        {
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            ExternalCaseNumber = @case.ExternalCaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
            Changed = @case.Updated ?? @case.Created,
            Labels = @case.Labels
                .OrderBy(static assignment => assignment.Label!.Name)
                .Select(static assignment => new LabelItem
                {
                    LabelId = assignment.LabelId,
                    Name = assignment.Label!.Name,
                    Color = assignment.Label!.Color,
                })
                .ToList(),
        });
    }
}
