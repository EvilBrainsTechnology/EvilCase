using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Data.DbContexts;

public interface IDbSession
{
    public ApplicationDbContext Current { get; }

    public Task<IDbContextTransaction> BeginTransaction(CancellationToken token);
}
