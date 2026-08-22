using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Who holds the contact as their default. Read by the detail, and by the delete that refuses one.
/// </summary>
internal static class DefaultContactQuery
{
    // User carries no tenant query filter, so this read names the tenant itself.
    public static IQueryable<User> WithDefaultContact(this IQueryable<User> users, IUserContext userContext, Guid contactId)
    {
        return users
            .Where(user => user.TenantId == userContext.TenantId)
            .Where(user => user.DefaultContactId == contactId);
    }
}
