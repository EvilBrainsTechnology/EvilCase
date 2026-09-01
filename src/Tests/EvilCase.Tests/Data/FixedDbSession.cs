using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FixedDbSession(ApplicationDbContext context) : IDbSession
{
    public ApplicationDbContext Current => context;

    public FakeDbContextTransaction? Transaction { get; private set; }

    public async Task<IDbContextTransaction> BeginTransaction(CancellationToken token)
    {
        this.Transaction = new FakeDbContextTransaction();

        return this.Transaction;
    }
}
