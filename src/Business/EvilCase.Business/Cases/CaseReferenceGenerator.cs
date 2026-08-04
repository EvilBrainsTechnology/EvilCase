using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReferenceGenerator(
    ApplicationDbContext dbContext,
    IOwnerContext ownerContext,
    IOptions<CaseSettings> settings,
    TimeProvider timeProvider) : ICaseReferenceGenerator
{
    public async Task<string> Next(CancellationToken cancellationToken = default)
    {
        var format = settings.Value.ReferenceFormat;
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var prefix = CaseReferenceSeries.Prefix(format, today);
        var ownerId = ownerContext.OwnerId;

        // Only today's marks: the counter starts over each day, and the prefix hits the index.
        var taken = await dbContext.Cases
            .Where(@case => @case.OwnerId == ownerId)
            .Where(@case => @case.InternalCaseReference.StartsWith(prefix))
            .Select(@case => @case.InternalCaseReference)
            .ToListAsync(cancellationToken);

        return CaseReferenceSeries.Format(format, today, CaseReferenceSeries.NextCounter(format, today, taken));
    }
}
