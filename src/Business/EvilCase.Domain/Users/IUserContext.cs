namespace EvilBrains.EvilCase.Domain.Users;

/// <summary>
/// The tenant and the signed-in user the current work belongs to. The one place either is resolved.
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

    /// <summary>
    /// Names the tenant and the user for work that runs outside a request, both at once and never one
    /// alone. Restores the previous pair on dispose.
    /// </summary>
    public IDisposable Enter(Guid tenantId, Guid userId);
}
