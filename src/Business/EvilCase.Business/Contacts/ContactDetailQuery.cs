using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Shapes the contact detail header.
/// </summary>
internal static class ContactDetailQuery
{
    /// <summary>
    /// Reads only the contact's own columns. <see cref="ContactDetail.IsDefault"/>, <see cref="ContactDetail.Cases"/>
    /// and <see cref="ContactDetail.Acts"/> are filled separately, with `with`.
    /// </summary>
    public static IQueryable<ContactDetail> AsDetail(this IQueryable<Contact> contacts, Guid id)
    {
        return contacts
            .Where(contact => contact.Id == id)
            .Select(contact => new ContactDetail
            {
                Id = contact.Id,
                Name = contact.Name,
                Kind = contact.Kind,
                DataBoxId = contact.DataBoxId,
                Address = contact.Address,
            });
    }

    // User carries no tenant query filter, so this read names the tenant itself.
    public static IQueryable<User> WithDefaultContact(this IQueryable<User> users, IUserContext userContext, Guid contactId)
    {
        return users
            .Where(user => user.TenantId == userContext.TenantId)
            .Where(user => user.DefaultContactId == contactId);
    }
}
