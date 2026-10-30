using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Data.DbContexts;

public interface IDbSession
{
    public ApplicationDbContext Current { get; }

    public Task<IDbContextTransaction> BeginTransaction(CancellationToken token);

    /// <summary>
    /// A transaction whose every read sees the database as it stood at its first read.
    /// </summary>
    public Task<IDbContextTransaction> BeginSnapshot(CancellationToken token);
}
