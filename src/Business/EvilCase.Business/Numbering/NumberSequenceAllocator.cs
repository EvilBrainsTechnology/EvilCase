using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberSequenceAllocator(ApplicationDbContext context, IOwnerContext owner) : INumberSequenceAllocator
{
    // Materialised whole: an operator on top of the query would wrap the statement in a subquery, and
    // PostgreSQL refuses one that writes.
    public async Task<int> Next(string scope, CancellationToken cancellationToken = default)
    {
        var taken = await context.Database
            .SqlQueryRaw<int>(NumberSequenceSql.TakeNext, owner.OwnerId, scope)
            .ToListAsync(cancellationToken);

        return taken.Single();
    }
}
