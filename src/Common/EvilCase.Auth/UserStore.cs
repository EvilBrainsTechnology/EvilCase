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

    public async Task<User?> FindById(Guid userId, CancellationToken cancellationToken)
    {
        return await dbSession.Current.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<DateTime?> RecordFailedLogin(Guid userId, int maxAttempts, DateTime lockoutEnd, CancellationToken cancellationToken)
    {
        // Every setter reads the stored column, never a number worked out from an earlier read.
        await dbSession.Current.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        user => user.FailedLoginAttempts,
                        user => user.FailedLoginAttempts + 1 >= maxAttempts ? 0 : user.FailedLoginAttempts + 1)
                    .SetProperty(
                        user => user.LockoutEnd,
                        user => user.FailedLoginAttempts + 1 >= maxAttempts ? lockoutEnd : user.LockoutEnd),
                cancellationToken);

        return await dbSession.Current.Users
            .Where(user => user.Id == userId)
            .Select(user => user.LockoutEnd)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task RecordSuccessfulLogin(Guid userId, CancellationToken cancellationToken)
    {
        await dbSession.Current.Users
            .Where(user => user.Id == userId)
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

    public async Task AddUser(User user, Contact defaultContact, CancellationToken cancellationToken)
    {
        // One save carries both rows; EF orders the contact before the user, which the user's key needs.
        dbSession.Current.Contacts.Add(defaultContact);
        dbSession.Current.Users.Add(user);

        await dbSession.Current.SaveChangesAsync(cancellationToken);
    }
}
