using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

// Updates go through ExecuteUpdate rather than through the change tracker: the context is registered
// with NoTracking and the entities are init-only records, so there is nothing to mutate anyway.
internal sealed class UserStore(ApplicationDbContext dbContext, ITenantContext tenantContext) : IUserStore
{
    public async Task<User?> FindByEmail(string normalizedEmail, CancellationToken cancellationToken) =>
        await dbContext.Users.SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public async Task<User?> FindById(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task RecordFailedLogin(Guid id, int failedAttempts, DateTime? lockoutEnd, DateTime now, CancellationToken cancellationToken)
    {
        _ = await dbContext.Users
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.FailedLoginAttempts, failedAttempts)
                    .SetProperty(user => user.LockoutEnd, lockoutEnd)
                    .SetProperty(user => user.Updated, now),
                cancellationToken);
    }

    public async Task RecordSuccessfulLogin(Guid id, DateTime now, CancellationToken cancellationToken)
    {
        _ = await dbContext.Users
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.FailedLoginAttempts, 0)
                    .SetProperty(user => user.LockoutEnd, (DateTime?)null)
                    .SetProperty(user => user.Updated, now),
                cancellationToken);
    }

    public async Task<bool> Any(CancellationToken cancellationToken) =>
        await dbContext.Users.AnyAsync(cancellationToken);

    public async Task CreateAccount(Account account, Tenant tenant, User user, Contact defaultContact, CancellationToken cancellationToken)
    {
        using var scope = tenantContext.Enter(tenant.Id);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Accounts.AddAsync(account, cancellationToken);
        await dbContext.Tenants.AddAsync(tenant, cancellationToken);
        await dbContext.Users.AddAsync(user, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        // The contact points at the user and the user back at the contact, so the pair cannot be inserted
        // in one statement; the column is filled once both rows exist.
        await dbContext.Contacts.AddAsync(defaultContact, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        _ = await dbContext.Users
            .Where(row => row.Id == user.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.DefaultContactId, defaultContact.Id), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
