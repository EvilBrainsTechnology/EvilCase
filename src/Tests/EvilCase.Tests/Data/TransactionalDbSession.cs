using System.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// Opens real transactions, so a test can see what a failed write leaves behind.
/// </summary>
internal sealed class TransactionalDbSession(ApplicationDbContext context) : IDbSession
{
    public ApplicationDbContext Current => context;

    public async Task<IDbContextTransaction> BeginTransaction(CancellationToken token)
    {
        return await context.Database.BeginTransactionAsync(token);
    }

    public async Task<IDbContextTransaction> BeginSnapshot(CancellationToken token)
    {
        return await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
    }
}
