using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal static class ExpiringSession
{
    /// <summary>
    /// A signed-in token store inside the renew-ahead window, so a renewal reaches the auth client.
    /// </summary>
    public static AccessTokenStore Store()
    {
        var tokens = new AccessTokenStore();

        tokens.SetAccessToken(new()
        {
            Token = "access",
            ExpiresAt = DateTime.UtcNow,
            Email = "user@example.com",
            Role = UserRole.User,
        });

        return tokens;
    }
}
