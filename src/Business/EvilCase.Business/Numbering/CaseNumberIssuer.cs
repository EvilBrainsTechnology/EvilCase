using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class CaseNumberIssuer(IDbSession dbSession) : ICaseNumberIssuer
{
    public async Task<string> NextCaseNumber(DateOnly date, CancellationToken cancellationToken)
    {
        var highest = await dbSession.Current.Cases
            .WithNumberPrefix(CaseNumberFormat.Prefix(date))
            .OrderByNumberDescending()
            .Select(@case => @case.CaseNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return CaseNumberFormat.Next(date, highest);
    }
}
