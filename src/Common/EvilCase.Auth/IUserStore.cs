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
    /// Counts a failed sign-in and locks the account until <paramref name="lockoutEnd"/> once
    /// <paramref name="maxAttempts"/> is reached, in one statement. Returns the row as it now stands.
    /// </summary>
    public Task<User?> RecordFailedLogin(Guid id, int maxAttempts, DateTime lockoutEnd, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the failure counter and any lockout.
    /// </summary>
    public Task RecordSuccessfulLogin(Guid id, CancellationToken cancellationToken);

    public Task<bool> Any(CancellationToken cancellationToken);

    /// <summary>
    /// Writes the user and its default contact in one save. A user without a default contact cannot exist.
    /// </summary>
    public Task Add(User user, Contact defaultContact, CancellationToken cancellationToken);
}
