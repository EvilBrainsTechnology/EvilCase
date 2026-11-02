using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class ActNumberIssuer(IDbSession dbSession) : IActNumberIssuer
{
    public async Task<string> NextActNumber(string caseNumber, DateOnly date, CancellationToken token)
    {
        var highest = await dbSession.Current.Acts
            .WithNumberPrefix(ActNumberFormat.Prefix(caseNumber, date))
            .OrderByNumberDescending()
            .Select(static act => act.ActNumber)
            .FirstOrDefaultAsync(token);

        return ActNumberFormat.Next(caseNumber, date, highest);
    }
}
