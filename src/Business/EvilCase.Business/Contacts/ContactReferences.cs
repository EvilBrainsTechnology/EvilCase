using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Whether anything still points at a contact, which is what stands between it and deletion (SDD-011).
/// </summary>
internal static class ContactReferences
{
    public static IQueryable<Contact> Referenced(this IQueryable<Contact> contacts)
    {
        return contacts.Where(static contact =>
            contact.IssuedActs.Count != 0
                || contact.AddressedActs.Count != 0
                || contact.AssignedExternalCaseNumbers.Count != 0
                || contact.AssignedExternalActNumbers.Count != 0);
    }
}
