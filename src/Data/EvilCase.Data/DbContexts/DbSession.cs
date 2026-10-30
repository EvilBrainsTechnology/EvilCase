using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Data.DbContexts;

internal sealed class DbSession(IServiceProvider serviceProvider) : IDbSession
{
    public ApplicationDbContext Current => serviceProvider.GetRequiredService<ApplicationDbContext>();

    public async Task<IDbContextTransaction> BeginTransaction(CancellationToken token)
    {
        return await this.Current.Database.BeginTransactionAsync(token);
    }

    public async Task<IDbContextTransaction> BeginSnapshot(CancellationToken token)
    {
        return await this.Current.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
    }
}
