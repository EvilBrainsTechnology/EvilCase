using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

// Updates go through ExecuteUpdate rather than through the change tracker: the context is registered
// with NoTracking and the entities are init-only records, so there is nothing to mutate anyway.
internal sealed class UserStore(ApplicationDbContext dbContext) : IUserStore
{
    public async Task<User?> FindByEmail(string normalizedEmail, CancellationToken cancellationToken) =>
        await dbContext.Users.SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public async Task<User?> FindById(long id, CancellationToken cancellationToken) =>
        await dbContext.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task RecordFailedLogin(long id, int failedAttempts, DateTime? lockoutEnd, DateTime now, CancellationToken cancellationToken)
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

    public async Task RecordSuccessfulLogin(long id, DateTime now, CancellationToken cancellationToken)
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

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
