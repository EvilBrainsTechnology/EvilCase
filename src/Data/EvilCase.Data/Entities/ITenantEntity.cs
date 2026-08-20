namespace EvilBrains.EvilCase.Data.Entities;

public interface ITenantEntity : IEntity
{
    public Guid TenantId { get; }
}
