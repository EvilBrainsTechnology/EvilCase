using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Who holds the contact as their default. Read by the detail, and by the delete that refuses one.
/// </summary>
internal static class DefaultContactQuery
{
    public static IQueryable<User> WithDefaultContact(this IQueryable<User> users, Guid contactId)
    {
        return users.Where(user => user.DefaultContactId == contactId);
    }

    /// <summary>
    /// The user's default contact. Every user has one (SDD-011), so this never comes back empty.
    /// </summary>
    public static Task<ContactListItem> DefaultContactOf(this IQueryable<User> users, Guid userId, CancellationToken token)
    {
        return users
            .WithId(userId)
            .Select(static user => new ContactListItem
            {
                ContactId = user.DefaultContact!.Id,
                Kind = user.DefaultContact!.Kind,
                Name = user.DefaultContact!.Name,
                DataBoxId = user.DefaultContact!.DataBoxId,
                Address = user.DefaultContact!.Address,
            })
            .SingleAsync(token);
    }
}
