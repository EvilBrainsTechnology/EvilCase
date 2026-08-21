using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class ActNumberIssuer(IDbSession dbSession) : IActNumberIssuer
{
    public async Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@case);

        var highest = await dbSession.Current.Acts
            .OfCaseWithNumberPrefix(@case.Id, ActNumberFormat.Prefix(@case.CaseNumber, date))
            .OrderByNumberDescending()
            .Select(act => act.ActNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return ActNumberFormat.Next(@case.CaseNumber, date, highest);
    }
}
