using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

internal sealed class UserStore(IDbSession dbSession) : IUserStore
{
    public async Task<User?> FindByEmail(string email, CancellationToken cancellationToken)
    {
        var normalized = EmailNormalizer.Normalize(email);

        return await dbSession.Current.Users.SingleOrDefaultAsync(user => user.Email == normalized, cancellationToken);
    }

    public async Task<User?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return await dbSession.Current.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<User?> RecordFailedLogin(Guid id, int maxAttempts, DateTime lockoutEnd, CancellationToken cancellationToken)
    {
        // Every setter reads the stored column, so the database counts and decides the lockout; two
        // concurrent misses starting from the same read would otherwise write the same number twice.
        await dbSession.Current.Users
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        user => user.FailedLoginAttempts,
                        user => user.FailedLoginAttempts + 1 >= maxAttempts ? 0 : user.FailedLoginAttempts + 1)
                    .SetProperty(
                        user => user.LockoutEnd,
                        user => user.FailedLoginAttempts + 1 >= maxAttempts ? lockoutEnd : user.LockoutEnd),
                cancellationToken);

        // Untracked: the update went round the change tracker, which still holds the row as it was read.
        return await dbSession.Current.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task RecordSuccessfulLogin(Guid id, CancellationToken cancellationToken)
    {
        await dbSession.Current.Users
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.FailedLoginAttempts, 0)
                    .SetProperty(user => user.LockoutEnd, (DateTime?)null),
                cancellationToken);
    }

    public async Task<bool> Any(CancellationToken cancellationToken)
    {
        return await dbSession.Current.Users.AnyAsync(cancellationToken);
    }

    public async Task Add(User user, Contact defaultContact, CancellationToken cancellationToken)
    {
        // One save carries both rows; EF orders the contact before the user, which the user's key needs.
        dbSession.Current.Contacts.Add(defaultContact);
        dbSession.Current.Users.Add(user);

        await dbSession.Current.SaveChangesAsync(cancellationToken);
    }
}
