using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Answers the contact detail, one row at most.
/// </summary>
internal static class ContactDetailQuery
{
    public static async Task<ContactDetail?> DetailOf(this IQueryable<Contact> contacts, Guid id, CancellationToken cancellationToken = default)
    {
        return await contacts
            .Where(contact => contact.Id == id)
            .Select(contact => new ContactDetail
            {
                Id = contact.Id,
                Name = contact.Name,
                Kind = contact.Kind,
                DataBoxId = contact.DataBoxId,
                Address = contact.Address,
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
