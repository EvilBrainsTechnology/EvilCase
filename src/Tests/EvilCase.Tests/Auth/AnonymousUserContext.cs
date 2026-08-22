using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// A caller with nobody signed in: what the sign-in endpoint and the startup seed write under.
/// </summary>
internal sealed class AnonymousUserContext : IUserContext
{
    public Guid UserId => throw new InvalidOperationException("The request has no signed-in user.");

    public Guid? UserIdOrDefault => null;
}
