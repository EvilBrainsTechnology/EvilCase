using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// A signed-in user for tests that write.
/// </summary>
internal sealed class StubUserContext : IUserContext
{
    public required Guid UserId { get; init; }
}
