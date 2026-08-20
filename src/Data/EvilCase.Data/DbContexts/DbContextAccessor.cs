using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Data.DbContexts;

internal sealed class DbContextAccessor(IServiceProvider serviceProvider) : IDbContextAccessor
{
    // The scope owns the context: the container creates it on the first read and disposes it with the scope.
    public ApplicationDbContext Current => serviceProvider.GetRequiredService<ApplicationDbContext>();
}
