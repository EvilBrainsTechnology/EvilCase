using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class CaseNumberReader(ApplicationDbContext context) : ICaseNumberReader
{
    public async Task<string> Read(long caseId, CancellationToken cancellationToken = default)
    {
        var number = await context.Cases
            .Where(@case => @case.Id == caseId)
            .Select(@case => @case.CaseNumber)
            .SingleOrDefaultAsync(cancellationToken);

        return number ?? throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"there is no case {caseId} to number an act under"));
    }
}
