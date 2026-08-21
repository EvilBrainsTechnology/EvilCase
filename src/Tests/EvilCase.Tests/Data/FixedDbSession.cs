using EvilBrains.EvilCase.Data.DbContexts;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FixedDbSession(ApplicationDbContext context) : IDbSession
{
    public ApplicationDbContext Current => context;
}
