using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FixedDbSession(ApplicationDbContext context) : IDbSession
{
    public ApplicationDbContext Current => context;

    public FakeDbContextTransaction? Transaction { get; private set; }

    public Task<IDbContextTransaction> BeginTransaction(CancellationToken cancellationToken)
    {
        this.Transaction = new FakeDbContextTransaction();

        return Task.FromResult<IDbContextTransaction>(this.Transaction);
    }
}
