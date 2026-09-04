using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

// Every query here ignores the tenant filter: sign-in, refresh and the seed run before a tenant is known.
internal sealed class UserStore(IDbSession dbSession) : IUserStore
{
    public async Task<User?> FindByEmail(string email, CancellationToken token)
    {
        var normalized = EmailNormalizer.Normalize(email);

        return await dbSession.Current.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(user => user.Email == normalized, token);
    }

    public async Task<User?> FindById(Guid userId, CancellationToken token)
    {
        return await dbSession.Current.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(user => user.Id == userId, token);
    }

    public async Task<DateTime?> RecordFailedLogin(Guid userId, int maxAttempts, DateTime lockoutEnd, CancellationToken token)
    {
        await dbSession.Current.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        static user => user.FailedLoginAttempts,
                        user => user.FailedLoginAttempts + 1 >= maxAttempts ? 0 : user.FailedLoginAttempts + 1)
                    .SetProperty(
                        static user => user.LockoutEnd,
                        user => user.FailedLoginAttempts + 1 >= maxAttempts ? lockoutEnd : user.LockoutEnd),
                token);

        return await dbSession.Current.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == userId)
            .Select(static user => user.LockoutEnd)
            .SingleOrDefaultAsync(token);
    }

    public async Task RecordSuccessfulLogin(Guid userId, CancellationToken token)
    {
        await dbSession.Current.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                static setters => setters
                    .SetProperty(static user => user.FailedLoginAttempts, 0)
                    .SetProperty(static user => user.LockoutEnd, (DateTime?)null),
                token);
    }

    public async Task<bool> Any(CancellationToken token)
    {
        return await dbSession.Current.Users.IgnoreQueryFilters().AnyAsync(token);
    }

    public async Task AddUser(User user, CancellationToken token)
    {
        dbSession.Current.Users.Add(user);

        await dbSession.Current.SaveChangesAsync(token);
    }
}
