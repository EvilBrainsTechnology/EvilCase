namespace EvilBrains.EvilCase.Data.Entities;

public interface IEntity
{
    public Guid Id { get; }

    public DateTime Created { get; }

    public DateTime? Updated { get; }
}
