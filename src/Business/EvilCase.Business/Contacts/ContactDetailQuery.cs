using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Shapes the contact detail header, one composable step per rule.
/// </summary>
internal static class ContactDetailQuery
{
    public static IQueryable<Contact> WithId(this IQueryable<Contact> contacts, Guid id)
    {
        return contacts.Where(contact => contact.Id == id);
    }

    /// <summary>
    /// Reads only the contact's own columns. <see cref="ContactDetail.IsDefault"/>, <see cref="ContactDetail.Cases"/>
    /// and <see cref="ContactDetail.Acts"/> are filled separately, with <c>with</c>.
    /// </summary>
    public static IQueryable<ContactDetail> AsDetail(this IQueryable<Contact> contacts)
    {
        return contacts.Select(contact => new ContactDetail
        {
            Id = contact.Id,
            Name = contact.Name,
            Kind = contact.Kind,
            DataBoxId = contact.DataBoxId,
            Address = contact.Address,
        });
    }
}
