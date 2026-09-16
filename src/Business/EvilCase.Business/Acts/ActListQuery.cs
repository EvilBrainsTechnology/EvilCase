using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Acts;

internal static class ActListQuery
{
    public static IQueryable<Act> MatchingSearch(this IQueryable<Act> acts, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return acts;

        var pattern = search.ContainsPattern();

        return acts.Where(act =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(act.Title), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || (act.Description != null
                    && EF.Functions.ILike(DatabaseFunctions.Unaccent(act.Description), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)));
    }

    public static IQueryable<Act> OfCase(this IQueryable<Act> acts, Guid? caseId)
    {
        return caseId is null ? acts : acts.Where(act => act.CaseId == caseId);
    }

    public static IQueryable<Act> WithContact(this IQueryable<Act> acts, Guid? contactId)
    {
        return contactId is null ? acts : acts.Where(act => act.ContactId == contactId);
    }

    public static IQueryable<Act> WithDirection(this IQueryable<Act> acts, ActDirection? direction)
    {
        return direction is null ? acts : acts.Where(act => act.Direction == direction);
    }

    public static IQueryable<Act> WithLabels(this IQueryable<Act> acts, IReadOnlyList<Guid> labelIds)
    {
        foreach (var labelId in labelIds)
            acts = acts.Where(act => act.Labels.Any(assignment => assignment.LabelId == labelId));

        return acts;
    }

    public static IQueryable<Act> WithinDates(this IQueryable<Act> acts, DateOnly? from, DateOnly? to)
    {
        if (from is not null)
            acts = acts.Where(act => act.Date >= from);

        if (to is not null)
            acts = acts.Where(act => act.Date <= to);

        return acts;
    }

    public static IQueryable<Act> InSortOrder(this IQueryable<Act> acts, ActSortKey sort, ListSortDirection direction)
    {
        return sort switch
        {
            ActSortKey.Date => acts.InKeyOrder(static act => act.Date, direction).ThenInWriteOrder(direction),
            ActSortKey.Changed => acts.InKeyOrder(static act => act.Updated ?? act.Created, direction).ThenInWriteOrder(direction),
            ActSortKey.Title => acts.InKeyOrder(static act => act.Title, direction).ThenInWriteOrder(direction),
            ActSortKey.ActNumber => acts.InKeyOrder(static act => act.ActNumber, direction).ThenInWriteOrder(direction),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unknown act sort key."),
        };
    }

    public static IQueryable<ActListItem> AsListItems(this IQueryable<Act> acts)
    {
        return acts.Select(static act => new ActListItem
        {
            ActId = act.Id,
            CaseId = act.CaseId,
            CaseNumber = act.Case!.CaseNumber,
            CaseTitle = act.Case!.Title,
            ActNumber = act.ActNumber,
            ExternalActNumber = act.ExternalActNumber,
            Direction = act.Direction,
            Title = act.Title,
            Date = act.Date,
            Changed = act.Updated ?? act.Created,
            ContactName = act.Contact!.Name,
            Labels = act.Labels
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
