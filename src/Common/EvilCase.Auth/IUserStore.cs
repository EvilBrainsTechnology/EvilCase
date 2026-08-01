using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Everything the authentication layer does to users. It and <see cref="IRefreshTokenStore"/> are the
/// only types here that touch the database, which is what lets the rest be tested without one.
/// </summary>
internal interface IUserStore
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Records a failed sign-in; a non-null <paramref name="lockoutEnd"/> locks the account.
    /// </summary>
    public Task RecordFailedLoginAsync(long id, int failedAttempts, DateTime? lockoutEnd, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the failure counter and any lockout.
    /// </summary>
    public Task RecordSuccessfulLoginAsync(long id, DateTime now, CancellationToken cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken);
}
