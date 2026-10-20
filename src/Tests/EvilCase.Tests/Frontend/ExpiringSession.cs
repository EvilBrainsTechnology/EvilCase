using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal static class ExpiringSession
{
    public static SessionStore Store()
    {
        var store = new SessionStore();

        store.Set(new()
        {
            ExpiresAt = DateTime.UtcNow,
            Email = "user@example.com",
            Role = UserRole.User,
        });

        return store;
    }
}
