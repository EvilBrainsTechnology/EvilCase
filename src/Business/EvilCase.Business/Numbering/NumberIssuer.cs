using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberIssuer(IDbSession dbSession) : INumberIssuer
{
    /// <summary>
    /// A taken number is a race with another writer, and the next read of the day already sees it.
    /// </summary>
    private const int Attempts = 5;

    public async Task<Case> InsertCase(DateOnly date, Func<string, Case> create, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(create);

        var attempt = 1;

        while (true)
        {
            var context = dbSession.Current;
            var taken = await context.Cases.CaseNumbersOfDay(date).ToListAsync(cancellationToken);
            var @case = create(CaseNumberFormat.Compose(date, CaseNumberFormat.NextSequence(date, taken)));

            var tracked = Tracked(context);
            context.Cases.Add(@case);

            try
            {
                await context.SaveChangesAsync(cancellationToken);

                return @case;
            }
            catch (DbUpdateException exception) when (attempt < Attempts && NumberConflict.IsTakenNumber(exception, nameof(Case.CaseNumber)))
            {
                DetachAddedSince(context, tracked);
                attempt++;
            }
        }
    }

    public async Task<Act> InsertAct(Case @case, DateOnly date, Func<string, Act> create, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@case);
        ArgumentNullException.ThrowIfNull(create);

        var attempt = 1;

        while (true)
        {
            var context = dbSession.Current;
            var taken = await context.Acts.ActNumbersOfCase(@case.Id).ToListAsync(cancellationToken);
            var act = create(ActNumberFormat.Compose(@case.CaseNumber, date, ActNumberFormat.NextSequence(date, taken)));

            var tracked = Tracked(context);
            context.Acts.Add(act);

            try
            {
                await context.SaveChangesAsync(cancellationToken);

                return act;
            }
            catch (DbUpdateException exception) when (attempt < Attempts && NumberConflict.IsTakenNumber(exception, nameof(Act.ActNumber)))
            {
                DetachAddedSince(context, tracked);
                attempt++;
            }
        }
    }

    public async Task<bool> IsCaseNumberFree(string number, Guid? excluding = null, CancellationToken cancellationToken = default) =>
        !await dbSession.Current.Cases.WithCaseNumber(number, excluding).AnyAsync(cancellationToken);

    public async Task<bool> IsActNumberFree(string number, Guid? excluding = null, CancellationToken cancellationToken = default) =>
        !await dbSession.Current.Acts.WithActNumber(number, excluding).AnyAsync(cancellationToken);

    private static HashSet<object> Tracked(ApplicationDbContext context) =>
        context.ChangeTracker.Entries().Select(entry => entry.Entity).ToHashSet(ReferenceEqualityComparer.Instance);

    // The factory may build a graph, and the add pulls all of it in; a retry takes back everything that attempt added.
    private static void DetachAddedSince(ApplicationDbContext context, HashSet<object> tracked)
    {
        var added = context.ChangeTracker.Entries().Where(entry => !tracked.Contains(entry.Entity)).ToList();

        foreach (var entry in added)
            entry.State = EntityState.Detached;
    }
}
