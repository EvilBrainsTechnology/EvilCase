using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Auth;

internal sealed class FakeUserStore : IUserStore
{
    private readonly List<User> users = [];

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
        var normalized = EmailNormalizer.Normalize(email);

        return this.users.Find(user => string.Equals(user.Email, normalized, StringComparison.Ordinal));
    }

    public async Task<User?> FindById(Guid userId, CancellationToken token)
    {
        return this.users.Find(user => user.Id == userId);
    }

    public async Task<DateTime?> RecordFailedLogin(Guid userId, int maxAttempts, DateTime lockoutEnd, CancellationToken token)
    {
        this.Replace(
            userId,
            user => user.FailedLoginAttempts + 1 >= maxAttempts
                ? user with { FailedLoginAttempts = 0, LockoutEnd = lockoutEnd }
                : user with { FailedLoginAttempts = user.FailedLoginAttempts + 1 });

        return this.GetUser(userId).LockoutEnd;
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
