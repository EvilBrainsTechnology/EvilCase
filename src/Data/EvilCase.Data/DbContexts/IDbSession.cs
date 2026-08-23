using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Data.DbContexts;

public interface IDbSession
{
    /// <summary>
    /// The context of the current DI scope, created on first use.
    /// </summary>
    public ApplicationDbContext Current { get; }

    /// <summary>
    /// Opens a transaction on the current context. The caller commits; disposing it uncommitted rolls back.
    /// </summary>
    public Task<IDbContextTransaction> BeginTransaction(CancellationToken cancellationToken);
}
