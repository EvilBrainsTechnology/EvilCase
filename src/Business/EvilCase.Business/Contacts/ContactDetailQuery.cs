using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal static class ContactDetailQuery
{
    /// <summary>
    /// Cases and Acts are left empty for the caller to fill.
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
