using EvilBrains.Dispose;
using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// A minimal <see cref="IUserContext"/> for tests that only need <see cref="Enter"/> to work.
/// </summary>
internal sealed class StubUserContext : IUserContext
{
    public Guid UserId => this.UserIdOrDefault ?? throw new InvalidOperationException("The request has no signed-in user.");

    public Guid? UserIdOrDefault { get; private set; }

    public IDisposable Enter(Guid userId)
    {
        var previous = this.UserIdOrDefault;
        this.UserIdOrDefault = userId;

        return new ActionDisposableScope(() => this.UserIdOrDefault = previous);
    }
}
