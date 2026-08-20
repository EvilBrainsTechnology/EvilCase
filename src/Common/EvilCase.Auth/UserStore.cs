using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

// A write goes through the change tracker, so TimestampInterceptor stamps Updated; the context is
// registered NoTracking, so a write reads its row with AsTracking() first.
internal sealed class UserStore(IDbContextAccessor accessor) : IUserStore
{
    public async Task<User?> FindByEmail(string email, CancellationToken cancellationToken)
    {
        var normalized = EmailNormalizer.Normalize(email);

        return await accessor.Current.Set<User>().SingleOrDefaultAsync(user => user.Email == normalized, cancellationToken);
    }

    public async Task<User?> FindById(Guid id, CancellationToken cancellationToken) =>
        await accessor.Current.Set<User>().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task RecordFailedLogin(Guid id, int failedAttempts, DateTime? lockoutEnd, CancellationToken cancellationToken)
    {
        var user = await accessor.Current.Users.AsTracking().SingleAsync(user => user.Id == id, cancellationToken);
        var entry = accessor.Current.Entry(user);

        entry.Property(user => user.FailedLoginAttempts).CurrentValue = failedAttempts;
        entry.Property(user => user.LockoutEnd).CurrentValue = lockoutEnd;

        _ = await accessor.Current.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordSuccessfulLogin(Guid id, CancellationToken cancellationToken)
    {
        var user = await accessor.Current.Users.AsTracking().SingleAsync(user => user.Id == id, cancellationToken);
        var entry = accessor.Current.Entry(user);

        entry.Property(user => user.FailedLoginAttempts).CurrentValue = 0;
        entry.Property(user => user.LockoutEnd).CurrentValue = null;

        _ = await accessor.Current.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> Any(CancellationToken cancellationToken) =>
        await accessor.Current.Set<User>().AnyAsync(cancellationToken);

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        accessor.Current.Add(user);
        await accessor.Current.SaveChangesAsync(cancellationToken);
    }
}
