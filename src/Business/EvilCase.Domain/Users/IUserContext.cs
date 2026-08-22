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
    /// Null for a health probe, the sign-in endpoint or a migration at startup.
    /// </summary>
    public Guid? UserIdOrDefault { get; }

    /// <summary>
    /// Names the user for work that runs outside a request. Restores the previous user on dispose.
    /// </summary>
    public IDisposable Enter(Guid userId);
}
