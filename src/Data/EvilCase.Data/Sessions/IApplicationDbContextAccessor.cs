using EvilBrains.EvilCase.Data.DbContexts;

namespace EvilBrains.EvilCase.Data.Sessions;

public interface IApplicationDbContextAccessor
{
    /// <summary>
    /// The context of the current DI scope, created on first use.
    /// </summary>
    public ApplicationDbContext Current { get; }
}
