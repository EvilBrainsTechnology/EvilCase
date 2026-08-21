using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class CaseNumberIssuer(IDbSession dbSession) : ICaseNumberIssuer
{
    public async Task<string> NextCaseNumber(DateOnly date, CancellationToken cancellationToken = default)
    {
        var highest = await dbSession.Current.Cases
            .WithNumberPrefix(CaseNumberFormat.Prefix(date))
            .HighestNumber()
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = (CaseNumberFormat.ParseOrDefault(highest)?.Sequence ?? 0) + 1;

        return CaseNumberFormat.Compose(date, sequence);
    }
}
