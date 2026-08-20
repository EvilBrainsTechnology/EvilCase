namespace EvilBrains.EvilCase.Domain.Tenancy;

/// <summary>
/// The tenant the current work belongs to. The one place tenancy is resolved.
/// </summary>
public interface ITenantContext
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
    /// Names the tenant for work that runs outside a request. Restores the previous tenant on dispose.
    /// </summary>
    public IDisposable Enter(Guid tenantId);
}
