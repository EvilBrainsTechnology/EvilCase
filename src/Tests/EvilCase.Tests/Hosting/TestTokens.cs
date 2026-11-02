using System.Net.Http.Headers;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

internal static class TestTokens
{
    public static AuthenticationHeaderValue BearerFrom(EvilCaseHost host, string email = AuthTestHarness.Email)
    {
        return new("Bearer", TokenFrom(host, email));
    }

    public static string TokenFrom(EvilCaseHost host, string email = AuthTestHarness.Email)
    {
        using var scope = host.Services.CreateScope();

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            Email = email,
            PasswordHash = "not-verified-here",
            Role = UserRole.Admin,
        };

        var tokens = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();

        return tokens.Generate(user, Guid.NewGuid()).Value;
    }
}
