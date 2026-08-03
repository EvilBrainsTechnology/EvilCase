namespace EvilBrains.EvilCase.Data;

/// <summary>
/// Who owns what the current request is about. The one place ownership is resolved.
/// </summary>
public interface IOwnerContext
{
    /// <summary>
    /// Throws when there is no authenticated caller.
    /// </summary>
    public long OwnerId { get; }

    /// <summary>
    /// Null for a health probe, the sign-in endpoint or a migration at startup.
    /// </summary>
    public long? OwnerIdOrDefault { get; }
}
