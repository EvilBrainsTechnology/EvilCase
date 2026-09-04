using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal static class ContactListQuery
{
    public static IQueryable<Contact> MatchingSearch(this IQueryable<Contact> contacts, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return contacts;

        var pattern = $"%{search.Trim().EscapeLikeWildcards()}%";

        return contacts.Where(contact =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(contact.Name), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || (contact.DataBoxId != null
                    && EF.Functions.ILike(DatabaseFunctions.Unaccent(contact.DataBoxId), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)));
    }

    public static IQueryable<Contact> InListOrder(this IQueryable<Contact> contacts)
    {
        return contacts
            .OrderBy(static contact => contact.Name)
            .ThenBy(static contact => contact.Id);
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
