namespace EvilBrains.EvilCase.Data.DbContexts;

public interface IDbContextAccessor
{
    /// <summary>
    /// The context of the current DI scope, created on first use.
    /// </summary>
    public ApplicationDbContext Current { get; }
}
