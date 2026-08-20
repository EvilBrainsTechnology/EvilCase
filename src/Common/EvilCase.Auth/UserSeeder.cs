using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Sessions;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Tenancy;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Auth;

internal sealed class UserSeeder(
    IApplicationDbSession session,
    IUserStore userStore,
    ITenantContext tenantContext,
    IOptions<AuthSettings> options,
    ILogger<UserSeeder> logger) : IUserSeeder
{
    public bool IsConfigured => options.Value.Seed is { Email.Length: > 0, Password.Length: > 0 };

    public async Task Seed(CancellationToken cancellationToken)
    {
        var seed = options.Value.Seed;

        if (seed?.Email is not { Length: > 0 } email || seed.Password is not { Length: > 0 } password)
            return;

        // Any user at all, not just this e-mail: the seed exists to make an empty deployment reachable,
        // and once someone can sign in it must not quietly reinstate an account that was removed.
        if (await userStore.Any(cancellationToken))
            return;

        var normalizedEmail = EmailNormalizer.Normalize(email);

        var account = new Account { Name = normalizedEmail };
        var tenant = new Tenant { AccountId = account.Id, Name = normalizedEmail };

        session.Add(account);
        session.Add(tenant);
        await session.SaveChanges(cancellationToken);

        using var scope = tenantContext.Enter(tenant.Id);

        // The two tables point at each other, so the user goes in without its contact and gets it in step three.
        var user = new User
        {
            TenantId = tenant.Id,
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(password),
            Role = UserRole.Admin,
        };

        await userStore.Add(user, cancellationToken);

        var contact = new Contact
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Kind = ContactKind.Person,
            Name = normalizedEmail,
        };

        session.Add(contact);
        await session.SaveChanges(cancellationToken);

        await userStore.SetDefaultContact(user.Id, contact.Id, cancellationToken);

        logger.LogInformation("No user existed, so the configured administrator {Email} was created in tenant {TenantId}", user.Email, tenant.Id);
    }
}
