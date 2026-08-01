using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// The whole of the authentication layer's contact with the users table. Standing in for it here is
/// what lets everything above be tested without a database.
/// </summary>
internal sealed class FakeUserStore : IUserStore
{
    private readonly List<User> users = [];

    public User Seed(User user)
    {
        var stored = user with { Id = this.users.Count + 1 };

        this.users.Add(stored);

        return stored;
    }

    public User Get(long id) => this.users.Single(user => user.Id == id);

    public Task<User?> FindByEmail(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(this.users.Find(user => string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal)));

    public Task<User?> FindById(long id, CancellationToken cancellationToken) =>
        Task.FromResult(this.users.Find(user => user.Id == id));

    public Task RecordFailedLogin(long id, int failedAttempts, DateTime? lockoutEnd, DateTime now, CancellationToken cancellationToken)
    {
        this.Replace(id, user => user with { FailedLoginAttempts = failedAttempts, LockoutEnd = lockoutEnd, Updated = now });

        return Task.CompletedTask;
    }

    public Task RecordSuccessfulLogin(long id, DateTime now, CancellationToken cancellationToken)
    {
        this.Replace(id, user => user with { FailedLoginAttempts = 0, LockoutEnd = null, Updated = now });

        return Task.CompletedTask;
    }

    public Task<bool> Any(CancellationToken cancellationToken) => Task.FromResult(this.users.Count > 0);

    public Task Add(User user, CancellationToken cancellationToken)
    {
        _ = this.Seed(user);

        return Task.CompletedTask;
    }

    private void Replace(long id, Func<User, User> update)
    {
        var index = this.users.FindIndex(user => user.Id == id);

        this.users[index] = update(this.users[index]);
    }
}
