using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class CaseNumberIssuer(IDbSession dbSession) : ICaseNumberIssuer
{
    public async Task<string> NextCaseNumber(DateOnly date, CancellationToken token)
    {
        // The unique index holds a stamped case's number too, so a reissue would collide with a row
        // nobody can see.
        var highest = await dbSession.Current.Cases
            .IncludingDeleted()
            .WithNumberPrefix(CaseNumberFormat.Prefix(date))
            .OrderByNumberDescending()
            .Select(static @case => @case.CaseNumber)
            .FirstOrDefaultAsync(token);

        return CaseNumberFormat.Next(date, highest);
    }
}
