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
}
