using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FixedDbSession(ApplicationDbContext context) : IDbSession
{
    public ApplicationDbContext Current => context;

    public RecordedDbContextTransaction? Transaction { get; private set; }

    public async Task<IDbContextTransaction> BeginTransaction(CancellationToken token)
    {
        // A context that never connects gets a substitute; every other one gets the real transaction,
        // which is what makes one cascade share one now().
        var transaction = context.Database.GetConnectionString() is null
            ? Substitute.For<IDbContextTransaction>()
            : await context.Database.BeginTransactionAsync(token);

        this.Transaction = new RecordedDbContextTransaction(transaction);

        return this.Transaction;
    }
}
