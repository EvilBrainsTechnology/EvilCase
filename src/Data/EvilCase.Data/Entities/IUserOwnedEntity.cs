namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A tenant entity that also belongs to the user who created it.
/// </summary>
public interface IUserOwnedEntity : ITenantEntity
{
    public Guid UserId { get; }
}
