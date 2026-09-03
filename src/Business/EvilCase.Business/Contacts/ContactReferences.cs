using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Whether any case or act still points at a contact, which is what stands between it and deletion
/// (SDD-011).
/// </summary>
internal static class ContactReferences
{
    public static IQueryable<Contact> Referenced(this IQueryable<Contact> contacts)
    {
        return contacts.Where(static contact => contact.Acts.Count != 0 || contact.Cases.Count != 0);
    }
}
