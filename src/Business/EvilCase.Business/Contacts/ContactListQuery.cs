using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal static class ContactListQuery
{
    public static IQueryable<Contact> MatchingSearch(this IQueryable<Contact> contacts, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return contacts;

        var pattern = search.ContainsPattern();

        return contacts.Where(contact =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(contact.Name), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || (contact.DataBoxId != null
                    && EF.Functions.ILike(DatabaseFunctions.Unaccent(contact.DataBoxId), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)));
    }

    public static IQueryable<Contact> WithKind(this IQueryable<Contact> contacts, ContactKind? kind)
    {
        return kind is null ? contacts : contacts.Where(contact => contact.Kind == kind);
    }

    public static IQueryable<Contact> InSortOrder(this IQueryable<Contact> contacts, ContactSortKey sort, ListSortDirection direction)
    {
        return sort switch
        {
            ContactSortKey.Name => contacts.InKeyOrder(static contact => contact.Name, direction).ThenInWriteOrder(direction),
            ContactSortKey.Changed => contacts.InKeyOrder(static contact => contact.Updated ?? contact.Created, direction).ThenInWriteOrder(direction),
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unknown contact sort key."),
        };
    }

    public static IQueryable<ContactListItem> AsListItems(this IQueryable<Contact> contacts)
    {
        return contacts.Select(static contact => new ContactListItem
        {
            ContactId = contact.Id,
            Kind = contact.Kind,
            Name = contact.Name,
            DataBoxId = contact.DataBoxId,
            Address = contact.Address,
        });
    }
}
