using System.Net.Http.Headers;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

internal static class TestTokens
{
    public const string Email = "user@evilcase.test";

    public static AuthenticationHeaderValue BearerFrom(EvilCaseHost host, string email = Email)
    {
        return new("Bearer", TokenFrom(host, email));
    }

    public static string TokenFrom(EvilCaseHost host, string email = Email)
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
