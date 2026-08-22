using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Data.DbContexts;

internal sealed class DbSession(IServiceProvider serviceProvider) : IDbSession
{
    // The scope owns the context: the container creates it on the first read and disposes it with the scope.
    public ApplicationDbContext Current => serviceProvider.GetRequiredService<ApplicationDbContext>();

    public Task<IDbContextTransaction> BeginTransaction(CancellationToken cancellationToken = default)
    {
        return this.Current.Database.BeginTransactionAsync(cancellationToken);
    }
}
