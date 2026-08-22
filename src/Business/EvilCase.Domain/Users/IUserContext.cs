namespace EvilBrains.EvilCase.Domain.Users;

/// <summary>
/// The signed-in user the current work belongs to. The one place the user is resolved.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Throws when the caller is not signed in.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Null for a health probe, the sign-in endpoint or a seed at startup.
    /// </summary>
    public Guid? UserIdOrDefault { get; }
}
