using System.Net.Http.Headers;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Mints access tokens through the very service the sign-in endpoint uses, so a signing configuration
/// that drifts apart from the one the bearer scheme validates against fails in a test rather than only
/// in a browser. Nothing here reaches the database.
/// </summary>
internal static class TestTokens
{
    public const string Email = "user@evilcase.test";

    public static AuthenticationHeaderValue BearerFrom(EvilCaseHost host, string email = Email) =>
        new("Bearer", TokenFrom(host, email));

    public static string TokenFrom(EvilCaseHost host, string email = Email)
    {
        ArgumentNullException.ThrowIfNull(host);

        using var scope = host.Services.CreateScope();

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            Email = email,
            PasswordHash = "not-verified-here",
            Role = UserRole.Admin,
        };

        return scope.ServiceProvider.GetRequiredService<IAuthTokenService>().Generate(user, Guid.NewGuid()).Value;
    }
}
