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

    private readonly List<Contact> contacts = [];

    public Contact SingleContact()
    {
        return this.contacts.Single();
    }

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

    public Task<User?> FindByEmail(string email, CancellationToken token)
    {
        var normalized = EmailNormalizer.Normalize(email);

        return Task.FromResult(this.users.Find(user => string.Equals(user.Email, normalized, StringComparison.Ordinal)));
    }

    public Task<User?> FindById(Guid userId, CancellationToken token)
    {
        return Task.FromResult(this.users.Find(user => user.Id == userId));
    }

    public Task<DateTime?> RecordFailedLogin(Guid userId, int maxAttempts, DateTime lockoutEnd, CancellationToken token)
    {
        this.Replace(
            userId,
            user => user.FailedLoginAttempts + 1 >= maxAttempts
                ? user with { FailedLoginAttempts = 0, LockoutEnd = lockoutEnd }
                : user with { FailedLoginAttempts = user.FailedLoginAttempts + 1 });

        return Task.FromResult(this.GetUser(userId).LockoutEnd);
    }

    public Task RecordSuccessfulLogin(Guid userId, CancellationToken token)
    {
        this.Replace(userId, user => user with { FailedLoginAttempts = 0, LockoutEnd = null });

        return Task.CompletedTask;
    }

    public Task<bool> Any(CancellationToken token)
    {
        return Task.FromResult(this.users.Count > 0);
    }

    public Task AddUser(User user, Contact defaultContact, CancellationToken token)
    {
        this.users.Add(user);
        this.contacts.Add(defaultContact);

        return Task.CompletedTask;
    }

    private void Replace(Guid userId, Func<User, User> update)
    {
        var index = this.users.FindIndex(user => user.Id == userId);

        this.users[index] = update(this.users[index]);
    }
}
