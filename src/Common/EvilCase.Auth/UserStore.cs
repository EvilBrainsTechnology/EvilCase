using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

// The login writes go through ExecuteUpdate rather than the change tracker: reading the row first
// would be a second round trip, so the statement sets Updated itself (SDD-018).
internal sealed class UserStore(IDbSession dbSession, TimeProvider timeProvider) : IUserStore
{
    public async Task<User?> FindByEmail(string email, CancellationToken cancellationToken)
    {
        var normalized = EmailNormalizer.Normalize(email);

        return await dbSession.Current.Users.SingleOrDefaultAsync(user => user.Email == normalized, cancellationToken);
    }

    public async Task<User?> FindById(Guid id, CancellationToken cancellationToken) =>
        await dbSession.Current.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task RecordFailedLogin(Guid id, int failedAttempts, DateTime? lockoutEnd, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        _ = await dbSession.Current.Users
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.FailedLoginAttempts, failedAttempts)
                    .SetProperty(user => user.LockoutEnd, lockoutEnd)
                    .SetProperty(user => user.Updated, now),
                cancellationToken);
    }

    public async Task RecordSuccessfulLogin(Guid id, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        _ = await dbSession.Current.Users
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.FailedLoginAttempts, 0)
                    .SetProperty(user => user.LockoutEnd, (DateTime?)null)
                    .SetProperty(user => user.Updated, now),
                cancellationToken);
    }

    public async Task<bool> Any(CancellationToken cancellationToken) =>
        await dbSession.Current.Users.AnyAsync(cancellationToken);

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        dbSession.Current.Users.Add(user);
        await dbSession.Current.SaveChangesAsync(cancellationToken);
    }
}
