using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Reads the header of one contact.
/// </summary>
internal static class ContactDetailQuery
{
    /// <summary>
    /// The header of one contact, or null where the tenant has no such contact.
    /// <see cref="ContactDetail.IsDefault"/> and <see cref="ContactDetail.Acts"/> are filled separately,
    /// with <c>with</c>.
    /// </summary>
    public static async Task<ContactDetail?> DetailOf(this IQueryable<Contact> contacts, Guid contactId, CancellationToken token)
    {
        return await contacts
            .WithId(contactId)
            .Select(static contact => new ContactDetail
            {
                ContactId = contact.Id,
                Name = contact.Name,
                Kind = contact.Kind,
                DataBoxId = contact.DataBoxId,
                Address = contact.Address,
            })
            .SingleOrDefaultAsync(token);
    }
}
