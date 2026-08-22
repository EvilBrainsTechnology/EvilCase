using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Shapes the contact list, one composable step per rule.
/// </summary>
public static class ContactListQuery
{
    /// <summary>
    /// Matches the name or the data box id, ignoring case and diacritics. A blank term narrows nothing.
    /// </summary>
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

    /// <summary>
    /// By name, the identifier breaking the tie so the order is total.
    /// </summary>
    public static IQueryable<Contact> InListOrder(this IQueryable<Contact> contacts)
    {
        return contacts
            .OrderBy(contact => contact.Name)
            .ThenBy(contact => contact.Id);
    }

    /// <summary>
    /// Reads only what a row shows, in one query.
    /// </summary>
    public static IQueryable<ContactListItem> AsListItems(this IQueryable<Contact> contacts)
    {
        return contacts.Select(contact => new ContactListItem
        {
            Id = contact.Id,
            Kind = contact.Kind,
            Name = contact.Name,
            DataBoxId = contact.DataBoxId,
            Address = contact.Address,
        });
    }
}
