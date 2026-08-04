using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberingSettingsReader(ApplicationDbContext context) : INumberingSettingsReader
{
    public async Task<NumberingSettings> Read(CancellationToken cancellationToken = default) =>
        await context.NumberingSettings.SingleOrDefaultAsync(cancellationToken)
            ?? new NumberingSettings
            {
                Id = NumberingSettings.SingletonId,
                CaseNumberPattern = NumberingDefaults.CaseNumberPattern,
                ActNumberPattern = NumberingDefaults.ActNumberPattern,
            };
}
