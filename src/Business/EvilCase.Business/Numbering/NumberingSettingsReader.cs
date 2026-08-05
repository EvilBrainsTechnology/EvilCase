using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberingSettingsReader(ApplicationDbContext context) : INumberingSettingsReader
{
    public async Task<NumberingPatterns> Read(CancellationToken cancellationToken = default) =>
        await context.NumberingSettings
            .Select(settings => new NumberingPatterns(settings.CaseNumberPattern, settings.ActNumberPattern))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new NumberingPatterns(NumberingDefaults.CaseNumberPattern, NumberingDefaults.ActNumberPattern);
}
