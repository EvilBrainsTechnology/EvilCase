using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Everything the authentication layer does to users. It and <see cref="IRefreshTokenStore"/> are the
/// only types here that touch the database, which is what lets the rest be tested without one.
/// </summary>
internal interface IUserStore
{
    /// <summary>
    /// Takes the e-mail as the caller has it; the store is what normalises.
    /// </summary>
    public Task<User?> FindByEmail(string email, CancellationToken token);

    public Task<User?> FindById(Guid userId, CancellationToken token);

    /// <summary>
    /// Counts a failed sign-in in one statement: the attempt that reaches <paramref name="maxAttempts"/>
    /// locks the account until <paramref name="lockoutEnd"/> and starts the counter over, so the first
    /// miss after an elapsed lockout does not lock it again. Returns the lockout the row carries
    /// afterwards, null where the row is gone.
    /// </summary>
    public Task<DateTime?> RecordFailedLogin(Guid userId, int maxAttempts, DateTime lockoutEnd, CancellationToken token);

    /// <summary>
    /// Clears the failure counter and any lockout.
    /// </summary>
    public Task RecordSuccessfulLogin(Guid userId, CancellationToken token);

    public Task<bool> Any(CancellationToken token);

    public Task AddUser(User user, CancellationToken token);
}
