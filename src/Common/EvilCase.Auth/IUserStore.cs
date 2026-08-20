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
    public Task<User?> FindByEmail(string email, CancellationToken cancellationToken);

    public Task<User?> FindById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Records a failed sign-in; a non-null <paramref name="lockoutEnd"/> locks the account.
    /// </summary>
    public Task RecordFailedLogin(Guid id, int failedAttempts, DateTime? lockoutEnd, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the failure counter and any lockout.
    /// </summary>
    public Task RecordSuccessfulLogin(Guid id, CancellationToken cancellationToken);

    public Task<bool> Any(CancellationToken cancellationToken);

    public Task Add(User user, CancellationToken cancellationToken);

    public Task SetDefaultContact(Guid userId, Guid contactId, CancellationToken cancellationToken);
}
