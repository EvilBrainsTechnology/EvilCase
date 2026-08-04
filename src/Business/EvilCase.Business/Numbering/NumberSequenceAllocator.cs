using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberSequenceAllocator(ApplicationDbContext context, IOwnerContext owner) : INumberSequenceAllocator
{
    public async Task<int> Next(string scope, CancellationToken cancellationToken = default) =>
        await context.Database
            .SqlQueryRaw<int>(NumberSequenceSql.TakeNext, owner.OwnerId, scope)
            .SingleAsync(cancellationToken);
}
