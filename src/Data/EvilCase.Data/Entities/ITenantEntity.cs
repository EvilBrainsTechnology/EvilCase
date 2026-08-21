namespace EvilBrains.EvilCase.Data.Entities;

public interface ITenantEntity : IEntity
{
    /// <summary>
    /// Filled by the write from the tenant the work runs under, so no creation sets it. A value set
    /// anyway is validated against that tenant rather than trusted.
    /// </summary>
    public Guid TenantId { get; }
}
