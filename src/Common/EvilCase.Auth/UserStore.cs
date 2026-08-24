using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

internal sealed class UserStore(IDbSession dbSession) : IUserStore
{
    public async Task<User?> FindByEmail(string email, CancellationToken token)
    {
        var normalized = EmailNormalizer.Normalize(email);

        // Sign-in runs before a tenant is known.
        return await dbSession.Current.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(user => user.Email == normalized, token);
    }

    public async Task<User?> FindById(Guid userId, CancellationToken token)
    {
        // The anonymous refresh endpoint calls this before a tenant is known.
        return await dbSession.Current.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(user => user.Id == userId, token);
    }

    public async Task<DateTime?> RecordFailedLogin(Guid userId, int maxAttempts, DateTime lockoutEnd, CancellationToken token)
    {
        // Sign-in runs before a tenant is known.
        // Every setter reads the stored column, never a number worked out from an earlier read.
        await dbSession.Current.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        user => user.FailedLoginAttempts,
                        user => user.FailedLoginAttempts + 1 >= maxAttempts ? 0 : user.FailedLoginAttempts + 1)
                    .SetProperty(
                        user => user.LockoutEnd,
                        user => user.FailedLoginAttempts + 1 >= maxAttempts ? lockoutEnd : user.LockoutEnd),
                token);

        return await dbSession.Current.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == userId)
            .Select(user => user.LockoutEnd)
            .SingleOrDefaultAsync(token);
    }

    public async Task RecordSuccessfulLogin(Guid userId, CancellationToken token)
    {
        // Sign-in runs before a tenant is known.
        await dbSession.Current.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.FailedLoginAttempts, 0)
                    .SetProperty(user => user.LockoutEnd, (DateTime?)null),
                token);
    }

    public async Task<bool> Any(CancellationToken token)
    {
        // The administrator seed at startup runs before a tenant is known.
        return await dbSession.Current.Users.IgnoreQueryFilters().AnyAsync(token);
    }

    public async Task AddUser(User user, Contact defaultContact, CancellationToken token)
    {
        // One save carries both rows; EF orders the contact before the user, which the user's key needs.
        dbSession.Current.Contacts.Add(defaultContact);
        dbSession.Current.Users.Add(user);

        await dbSession.Current.SaveChangesAsync(token);
    }
}
