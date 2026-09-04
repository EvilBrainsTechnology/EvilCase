namespace EvilBrains.EvilCase.Domain.Users;

/// <summary>
/// The one place the tenant and the user are resolved.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Throws when there is no tenant.
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    /// Null for a health probe, the sign-in endpoint or a migration at startup.
    /// </summary>
    public Guid? TenantIdOrDefault { get; }

    /// <summary>
    /// Throws when the caller is not signed in.
    /// </summary>
    public Guid UserId { get; }

    public Guid? UserIdOrDefault { get; }

    /// <summary>
    /// Restores the previous pair on dispose.
    /// </summary>
    public IDisposable Enter(Guid tenantId, Guid userId);
}
