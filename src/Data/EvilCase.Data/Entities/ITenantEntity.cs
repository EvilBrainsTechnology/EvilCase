namespace EvilBrains.EvilCase.Data.Entities;

public interface ITenantEntity : IEntity
{
    /// <summary>
    /// Stamped by UserWriteInterceptor; a preset value is validated, not trusted.
    /// </summary>
    public Guid TenantId { get; }
}
