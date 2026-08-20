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

    public Account? Account { get; private set; }

    public Tenant? Tenant { get; private set; }

    public Contact? DefaultContact { get; private set; }

    public User Seed(User user)
    {
        this.users.Add(user);

        return user;
    }

    public User Get(Guid id) => this.users.Single(user => user.Id == id);

    public Task<User?> FindByEmail(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(this.users.Find(user => string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal)));

    public Task<User?> FindById(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(this.users.Find(user => user.Id == id));

    public Task RecordFailedLogin(Guid id, int failedAttempts, DateTime? lockoutEnd, DateTime now, CancellationToken cancellationToken)
    {
        this.Replace(id, user => user with { FailedLoginAttempts = failedAttempts, LockoutEnd = lockoutEnd, Updated = now });

        return Task.CompletedTask;
    }

    public Task RecordSuccessfulLogin(Guid id, DateTime now, CancellationToken cancellationToken)
    {
        this.Replace(id, user => user with { FailedLoginAttempts = 0, LockoutEnd = null, Updated = now });

        return Task.CompletedTask;
    }

    public Task<bool> Any(CancellationToken cancellationToken) => Task.FromResult(this.users.Count > 0);

    public Task CreateAccount(Account account, Tenant tenant, User user, Contact defaultContact, CancellationToken cancellationToken)
    {
        this.Account = account;
        this.Tenant = tenant;
        this.DefaultContact = defaultContact;

        _ = this.Seed(user with { DefaultContactId = defaultContact.Id });

        return Task.CompletedTask;
    }

    private void Replace(Guid id, Func<User, User> update)
    {
        var index = this.users.FindIndex(user => user.Id == id);

        this.users[index] = update(this.users[index]);
    }
}
