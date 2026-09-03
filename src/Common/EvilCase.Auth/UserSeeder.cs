using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Auth;

internal sealed class UserSeeder(
    IDbSession dbSession,
    IUserStore userStore,
    IUserContext userContext,
    IOptions<AuthSettings> options,
    ILogger<UserSeeder> logger) : IUserSeeder
{
    public bool IsConfigured => options.Value.Seed is { Email.Length: > 0, Password.Length: > 0 };

    public async Task SeedUser(CancellationToken token)
    {
        var seed = options.Value.Seed;

        if (seed?.Email is not { Length: > 0 } email || seed.Password is not { Length: > 0 } password)
            return;

        // Any user at all, not just this e-mail: the seed exists to make an empty deployment reachable,
        // and once someone can sign in it must not quietly reinstate an account that was removed.
        if (await userStore.Any(token))
            return;

        var normalizedEmail = EmailNormalizer.Normalize(email);

        var account = new Account { Name = normalizedEmail };
        var tenant = new Tenant { AccountId = account.Id, Name = normalizedEmail };

        dbSession.Current.Accounts.Add(account);
        dbSession.Current.Tenants.Add(tenant);
        await dbSession.Current.SaveChangesAsync(token);

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(password),
            Role = UserRole.Admin,
        };

        using var scope = userContext.Enter(tenant.Id, user.Id);

        await userStore.AddUser(user, token);

        logger.LogInformation("No user existed, so the configured administrator {Email} was created in tenant {TenantId}", user.Email, tenant.Id);
    }
}
