using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class IssuedNumberReader(ApplicationDbContext context, IOwnerContext owner) : IIssuedNumberReader
{
    public async Task<int> HighestCaseNumber(NumberSeries series, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(series);

        var prefix = series.LikePrefix;

        var numbers = await context.Cases
            .Where(@case => @case.OwnerId == owner.OwnerId && EF.Functions.Like(@case.CaseNumber, prefix, NumberSeries.LikeEscape))
            .Select(@case => @case.CaseNumber)
            .ToListAsync(cancellationToken);

        return series.Highest(numbers);
    }

    // The case is the whole scope, and the caller reached it through ICaseNumberReader, which is where
    // it was scoped to the owner.
    public async Task<int> HighestActNumber(long caseId, NumberSeries series, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(series);

        var prefix = series.LikePrefix;

        var numbers = await context.Acts
            .Where(act => act.CaseId == caseId && EF.Functions.Like(act.ActNumber, prefix, NumberSeries.LikeEscape))
            .Select(act => act.ActNumber)
            .ToListAsync(cancellationToken);

        return series.Highest(numbers);
    }
}
