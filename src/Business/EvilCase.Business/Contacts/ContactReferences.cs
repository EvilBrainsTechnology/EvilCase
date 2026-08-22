using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Whether anything still points at a contact, which is what stands between it and deletion (SDD-011).
/// </summary>
public static class ContactReferences
{
    public static async Task<bool> IsContactReferenced(this ApplicationDbContext context, Guid contactId, CancellationToken cancellationToken = default)
    {
        return await context.Contacts
            .Where(contact => contact.Id == contactId)
            .AnyAsync(
                contact => contact.IssuedActs.Count != 0
                    || contact.AddressedActs.Count != 0
                    || contact.AssignedExternalCaseNumbers.Count != 0
                    || contact.AssignedExternalActNumbers.Count != 0,
                cancellationToken);
    }
}
