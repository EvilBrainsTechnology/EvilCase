using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Sessions;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

// Updates go through ExecuteUpdate rather than through the change tracker: the session queries with
// NoTracking and the entities are init-only records, so there is nothing to mutate anyway.
internal sealed class UserStore(IApplicationDbSession session, TimeProvider timeProvider) : IUserStore
{
    public async Task<User?> FindByEmail(string email, CancellationToken cancellationToken)
    {
        var normalized = EmailNormalizer.Normalize(email);

        return await session.Query<User>().SingleOrDefaultAsync(user => user.Email == normalized, cancellationToken);
    }

    public async Task<User?> FindById(Guid id, CancellationToken cancellationToken) =>
        await session.Query<User>().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task RecordFailedLogin(Guid id, int failedAttempts, DateTime? lockoutEnd, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        _ = await session.Query<User>()
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

        _ = await session.Query<User>()
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.FailedLoginAttempts, 0)
                    .SetProperty(user => user.LockoutEnd, (DateTime?)null)
                    .SetProperty(user => user.Updated, now),
                cancellationToken);
    }

    public async Task<bool> Any(CancellationToken cancellationToken) =>
        await session.Query<User>().AnyAsync(cancellationToken);

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        session.Add(user);
        await session.SaveChanges(cancellationToken);
    }

    public async Task SetDefaultContact(Guid userId, Guid contactId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        _ = await session.Query<User>()
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.DefaultContactId, contactId)
                    .SetProperty(user => user.Updated, now),
                cancellationToken);
    }
}
