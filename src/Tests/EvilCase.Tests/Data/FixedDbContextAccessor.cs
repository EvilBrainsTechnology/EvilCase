using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Sessions;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FixedDbContextAccessor(ApplicationDbContext context) : IApplicationDbContextAccessor
{
    public ApplicationDbContext Current => context;
}
