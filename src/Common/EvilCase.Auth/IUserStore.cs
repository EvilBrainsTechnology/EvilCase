using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

internal interface IUserStore
{
    public Task<User?> FindByEmail(string email, CancellationToken token);

    public Task<User?> FindById(Guid userId, CancellationToken token);

    /// <summary>
    /// Reaching maxAttempts sets the lockout and resets the counter; returns the row's lockout afterwards,
    /// null where the row is gone.
    /// </summary>
    public Task<DateTime?> RecordFailedLogin(Guid userId, int maxAttempts, DateTime lockoutEnd, CancellationToken token);

    public Task RecordSuccessfulLogin(Guid userId, CancellationToken token);

    public Task<bool> Any(CancellationToken token);

    public Task AddUser(User user, CancellationToken token);
}
