using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Auth;

internal sealed class FakeUserStore : IUserStore
{
    private readonly List<User> users = [];

    /// <summary>
    /// Answers FindByEmail in place of the stored row, as a read taken before a concurrent write.
    /// </summary>
    public User? StaleRead { get; set; }

    public User SeedUser(User user)
    {
        this.users.Add(user);

        return user;
    }

    public User GetUser(Guid userId)
    {
        return this.users.Single(user => user.Id == userId);
    }

    public User Single()
    {
        return this.users.Single();
    }

    public async Task<User?> FindByEmail(string email, CancellationToken token)
    {
        if (this.StaleRead is { } stale)
            return stale;

        var normalized = EmailNormalizer.Normalize(email);

        return this.users.Find(user => string.Equals(user.Email, normalized, StringComparison.Ordinal));
    }

    public async Task<User?> FindById(Guid userId, CancellationToken token)
    {
        return this.users.Find(user => user.Id == userId);
    }

    public async Task<bool> CountLoginAttempt(Guid userId, int maxAttempts, DateTime now, DateTime lockoutEnd, CancellationToken token)
    {
        if (this.GetUser(userId).LockoutEnd > now)
            return false;

        this.Replace(
            userId,
            user => user.FailedLoginAttempts + 1 >= maxAttempts
                ? user with { FailedLoginAttempts = 0, LockoutEnd = lockoutEnd }
                : user with { FailedLoginAttempts = user.FailedLoginAttempts + 1 });

        return true;
    }

    public async Task RecordSuccessfulLogin(Guid userId, CancellationToken token)
    {
        this.Replace(userId, static user => user with { FailedLoginAttempts = 0, LockoutEnd = null });
    }

    public async Task<bool> Any(CancellationToken token)
    {
        return this.users.Count > 0;
    }

    public async Task AddUser(User user, CancellationToken token)
    {
        this.users.Add(user);
    }

    private void Replace(Guid userId, Func<User, User> update)
    {
        var index = this.users.FindIndex(user => user.Id == userId);

        this.users[index] = update(this.users[index]);
    }
}
