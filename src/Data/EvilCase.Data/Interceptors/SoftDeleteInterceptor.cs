using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

/// <summary>
/// Turns the removal of an <see cref="ISoftDeleteEntity"/> into its stamp, so <c>Remove</c> never
/// takes a row for good. A set-based delete runs outside every interceptor and reaches for
/// <see cref="SoftDeleteExtensions.ExecuteSoftDelete{TEntity}"/> itself (SDD-018).
/// </summary>
internal sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await Stamp(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static async Task Stamp(DbContext? context, CancellationToken token)
    {
        var entries = context?.ChangeTracker.Entries()
            .Where(static entry => entry.Entity is ISoftDeleteEntity)
            .Where(static entry => entry.State == EntityState.Deleted)
            .ToList();

        if (entries is not { Count: > 0 })
            return;

        // The database's own clock, the one the Created and Updated trigger reads.
        var deleted = await context!.Database
            .SqlQuery<DateTime>($"SELECT now() AS \"Value\"")
            .SingleAsync(token);

        foreach (var entry in entries)
        {
            // Unchanged first: EntityState.Modified would send every column, and the removed entity
            // carries only what the caller read into it.
            entry.State = EntityState.Unchanged;
            entry.Property(nameof(ISoftDeleteEntity.Deleted)).CurrentValue = deleted;
        }
    }
}
