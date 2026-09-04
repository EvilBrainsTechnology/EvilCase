using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Contacts;

internal static class ContactReferences
{
    public static IQueryable<Contact> Referenced(this IQueryable<Contact> contacts)
    {
        return contacts.Where(static contact => contact.Acts.Count != 0 || contact.Cases.Count != 0);
    }
}
