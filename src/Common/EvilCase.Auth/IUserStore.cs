using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Everything the authentication layer does to users. It and <see cref="IRefreshTokenStore"/> are the
/// only types here that touch the database, which is what lets the rest be tested without one.
/// </summary>
internal interface IUserStore
{
    public Task<User?> FindByEmail(string normalizedEmail, CancellationToken cancellationToken);

    public Task<User?> FindById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Records a failed sign-in; a non-null <paramref name="lockoutEnd"/> locks the account.
    /// </summary>
    public Task RecordFailedLogin(Guid id, int failedAttempts, DateTime? lockoutEnd, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the failure counter and any lockout.
    /// </summary>
    public Task RecordSuccessfulLogin(Guid id, DateTime now, CancellationToken cancellationToken);

    public Task<bool> Any(CancellationToken cancellationToken);

    /// <summary>
    /// Writes the first account, its tenant, the administrator and the administrator's default contact
    /// in one transaction, under the tenant the seeder names.
    /// </summary>
    public Task CreateAccount(Account account, Tenant tenant, User user, Contact defaultContact, CancellationToken cancellationToken);
}
