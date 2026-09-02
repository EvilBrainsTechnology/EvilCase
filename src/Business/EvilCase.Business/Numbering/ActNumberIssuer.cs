using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class ActNumberIssuer(IDbSession dbSession) : IActNumberIssuer
{
    public async Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken token)
    {
        // The unique index holds a stamped act's number too, so a reissue would collide with a row
        // nobody can see.
        var highest = await dbSession.Current.Acts
            .IncludingDeleted()
            .OfCaseWithNumberPrefix(@case.Id, ActNumberFormat.Prefix(@case.CaseNumber, date))
            .OrderByNumberDescending()
            .Select(static act => act.ActNumber)
            .FirstOrDefaultAsync(token);

        return ActNumberFormat.Next(@case.CaseNumber, date, highest);
    }
}
