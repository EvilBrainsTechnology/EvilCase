namespace EvilBrains.EvilCase.Data.Entities;

public interface IUserOwnedEntity : ITenantEntity
{
    public Guid UserId { get; }
}
