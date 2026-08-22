using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Tests.Auth;

internal sealed class StubUserContext : IUserContext
{
    public required Guid UserId { get; init; }

    public Guid? UserIdOrDefault => this.UserId;
}
