namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A tenant entity that also belongs to the user who created it.
/// </summary>
public interface IUserOwnedEntity : ITenantEntity
{
    /// <summary>
    /// Filled by the write from the user the work runs under, so no creation sets it. A value set
    /// anyway is validated against that user rather than trusted.
    /// </summary>
    public Guid UserId { get; }
}
