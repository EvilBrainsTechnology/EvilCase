using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberSequenceAllocator(ApplicationDbContext context, IOwnerContext owner) : INumberSequenceAllocator
{
    // Materialised whole: an operator on top of the query would compose over the statement, and EF Core
    // refuses that over SQL it cannot compose — "'FromSql' or 'SqlQuery' was called with non-composable
    // SQL and with a query composing over it".
    public async Task<int> Next(string scope, CancellationToken cancellationToken = default)
    {
        var taken = await context.Database
            .SqlQueryRaw<int>(NumberSequenceSql.TakeNext, owner.OwnerId, scope)
            .ToListAsync(cancellationToken);

        return taken.Single();
    }
}
