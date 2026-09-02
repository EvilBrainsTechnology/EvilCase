namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// An entity a delete stamps instead of removing. The stamp comes from the database clock, in one
/// value for everything a single transaction takes (SDD-018).
/// </summary>
public interface ISoftDeleteEntity : IEntity
{
    public DateTime? Deleted { get; }
}
