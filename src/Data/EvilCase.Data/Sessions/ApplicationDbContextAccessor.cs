using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Data.Sessions;

internal sealed class ApplicationDbContextAccessor(IServiceProvider serviceProvider) : IApplicationDbContextAccessor
{
    // The scope owns the context: the container creates it on the first read and disposes it with the scope.
    public ApplicationDbContext Current => serviceProvider.GetRequiredService<ApplicationDbContext>();
}
