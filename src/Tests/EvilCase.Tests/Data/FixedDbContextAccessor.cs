using EvilBrains.EvilCase.Data.DbContexts;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FixedDbContextAccessor(ApplicationDbContext context) : IDbContextAccessor
{
    public ApplicationDbContext Current => context;
}
