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

    public User Seed(User user)
    {
        this.users.Add(user);

        return user;
    }

    public User Get(Guid id)
    {
        return this.users.Single(user => user.Id == id);
    }

    public User Single()
    {
        return this.users.Single();
    }

    public Task<User?> FindByEmail(string email, CancellationToken cancellationToken)
    {
        var normalized = EmailNormalizer.Normalize(email);

        return Task.FromResult(this.users.Find(user => string.Equals(user.Email, normalized, StringComparison.Ordinal)));
    }

    public Task<User?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.users.Find(user => user.Id == id));
    }

    public Task<DateTime?> RecordFailedLogin(Guid id, int maxAttempts, DateTime lockoutEnd, CancellationToken cancellationToken)
    {
        this.Replace(
            id,
            user => user.FailedLoginAttempts + 1 >= maxAttempts
                ? user with { FailedLoginAttempts = 0, LockoutEnd = lockoutEnd }
                : user with { FailedLoginAttempts = user.FailedLoginAttempts + 1 });

        return Task.FromResult(this.Get(id).LockoutEnd);
    }

    public Task RecordSuccessfulLogin(Guid id, CancellationToken cancellationToken)
    {
        this.Replace(id, user => user with { FailedLoginAttempts = 0, LockoutEnd = null });

        return Task.CompletedTask;
    }

    public Task<bool> Any(CancellationToken cancellationToken)
    {
        return Task.FromResult(this.users.Count > 0);
    }

    public Task Add(User user, Contact defaultContact, CancellationToken cancellationToken)
    {
        this.users.Add(user);
        this.contacts.Add(defaultContact);

        return Task.CompletedTask;
    }

    private void Replace(Guid id, Func<User, User> update)
    {
        var index = this.users.FindIndex(user => user.Id == id);

        this.users[index] = update(this.users[index]);
    }
}
