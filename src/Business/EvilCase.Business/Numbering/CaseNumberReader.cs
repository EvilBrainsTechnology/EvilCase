using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class CaseNumberReader(ApplicationDbContext context, IOwnerContext owner) : ICaseNumberReader
{
    public async Task<string> Read(long caseId, CancellationToken cancellationToken = default)
    {
        var number = await context.Cases
            .Where(@case => @case.Id == caseId && @case.OwnerId == owner.OwnerId)
            .Select(@case => @case.CaseNumber)
            .SingleOrDefaultAsync(cancellationToken);

        return number ?? throw CaseNotFoundException.For(caseId);
    }
}
