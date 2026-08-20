namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A tenant entity that also belongs to the user who created it. A <see cref="Contact"/> is the one
/// tenant entity outside this: shared across the tenant, owned by nobody.
/// </summary>
public interface IUserOwnedEntity : ITenantEntity
{
    public Guid UserId { get; }
}
