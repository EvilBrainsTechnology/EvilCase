using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

internal interface IUserStore
{
    public Task<User?> FindByEmail(string email, CancellationToken token);

    public Task<User?> FindById(Guid userId, CancellationToken token);

    /// <summary>
    /// Counts a sign-in attempt in one statement unless the account is locked at now; reaching maxAttempts
    /// sets the lockout and resets the counter. False where the account is locked or the row is gone.
    /// </summary>
    public Task<bool> CountLoginAttempt(Guid userId, int maxAttempts, DateTime now, DateTime lockoutEnd, CancellationToken token);

    public Task RecordSuccessfulLogin(Guid userId, CancellationToken token);

    public Task<bool> Any(CancellationToken token);

    public Task AddUser(User user, CancellationToken token);
}
