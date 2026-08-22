using EvilBrains.EvilCase.Data.Entities;

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
}
