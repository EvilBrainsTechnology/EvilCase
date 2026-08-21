using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class ActNumberIssuer(IDbSession dbSession) : IActNumberIssuer
{
    /// <summary>
    /// A taken number is a race with another writer, and the next read of the day already sees it.
    /// </summary>
    private const int Attempts = 5;

    public async Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@case);

        var taken = await dbSession.Current.Acts
            .WithNumberOfDay(@case.CaseNumber, date)
            .Select(act => act.ActNumber)
            .ToListAsync(cancellationToken);

        return ActNumberFormat.Compose(@case.CaseNumber, date, ActNumberFormat.NextSequence(@case.CaseNumber, date, taken));
    }

    public async Task Save(Act act, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(act);

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
                    && NumberConflict.IsActNumberConflict(exception)
                    && ActNumberFormat.ParseOrDefault(act.ActNumber) is not null)
            {
                // Somebody took the number between the read and the insert; the entries stay Added, so the
                // next sequence of the same case and day goes in and the same insert runs again.
                var parts = ActNumberFormat.Parse(act.ActNumber);

                act.ActNumber = ActNumberFormat.Compose(parts.CaseNumber, parts.Date, parts.Sequence + 1);
                attempt++;
            }
        }
    }
}
