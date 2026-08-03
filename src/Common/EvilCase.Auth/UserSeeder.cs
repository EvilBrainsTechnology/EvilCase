using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Auth;

internal sealed class UserSeeder(
    IUserStore userStore,
    IOptions<AuthSettings> options,
    TimeProvider timeProvider,
    ILogger<UserSeeder> logger) : IUserSeeder
{
    public async Task Seed(CancellationToken cancellationToken)
    {
        var seed = options.Value.Seed;

        if (seed?.Email is not { Length: > 0 } email || seed.Password is not { Length: > 0 } password)
            return;

        // Any user at all, not just this e-mail: the seed exists to make an empty deployment reachable,
        // and once someone can sign in it must not quietly reinstate an account that was removed.
        if (await userStore.Any(cancellationToken))
            return;

        var user = new User
        {
            Email = EmailNormalizer.Normalize(email),
            PasswordHash = PasswordHasher.Hash(password),
            Role = UserRole.Admin,
            Created = timeProvider.GetUtcNow().UtcDateTime,
        };

        await userStore.Add(user, cancellationToken);

        logger.LogInformation("No user existed, so the configured administrator {Email} was created", user.Email);
    }
}
