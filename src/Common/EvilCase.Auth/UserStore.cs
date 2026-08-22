using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

// The login writes go through ExecuteUpdate rather than the change tracker: reading the row first
// would be a second round trip. Updated comes from the database trigger (SDD-018).
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

    public async Task RecordFailedLogin(Guid id, int failedAttempts, DateTime? lockoutEnd, CancellationToken cancellationToken)
    {
        await dbSession.Current.Users
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.FailedLoginAttempts, failedAttempts)
                    .SetProperty(user => user.LockoutEnd, lockoutEnd),
                cancellationToken);
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
