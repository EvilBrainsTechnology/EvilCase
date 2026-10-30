using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FixedDbSession(ApplicationDbContext context) : IDbSession
{
    public ApplicationDbContext Current => context;

    public async Task<IDbContextTransaction> BeginTransaction(CancellationToken token)
    {
        return new FakeDbContextTransaction();
    }

    public async Task<IDbContextTransaction> BeginSnapshot(CancellationToken token)
    {
        return new FakeDbContextTransaction();
    }
}
