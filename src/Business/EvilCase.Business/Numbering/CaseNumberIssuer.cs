using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class CaseNumberIssuer(IDbSession dbSession) : ICaseNumberIssuer
{
    /// <summary>
    /// A taken number is a race with another writer, and the next read of the day already sees it.
    /// </summary>
    private const int Attempts = 5;

    public async Task<string> NextCaseNumber(DateOnly date, CancellationToken cancellationToken = default)
    {
        var taken = await dbSession.Current.Cases
            .WithNumberOfDay(date)
            .Select(@case => @case.CaseNumber)
            .ToListAsync(cancellationToken);

        return CaseNumberFormat.Compose(date, CaseNumberFormat.NextSequence(date, taken));
    }

    public async Task Save(Case @case, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@case);

        var attempt = 1;

        while (true)
        {
            try
            {
                await dbSession.Current.SaveChangesAsync(cancellationToken);

                return;
            }
            catch (DbUpdateException exception)
                when (attempt < Attempts
                    && NumberConflict.IsCaseNumberConflict(exception)
                    && CaseNumberFormat.ParseOrDefault(@case.CaseNumber) is not null)
            {
                // Somebody took the number between the read and the insert; the entries stay Added, so the
                // next sequence of the same day goes in and the same insert runs again.
                var parts = CaseNumberFormat.Parse(@case.CaseNumber);

                @case.CaseNumber = CaseNumberFormat.Compose(parts.Date, parts.Sequence + 1);
                attempt++;
            }
        }
    }
}
