using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

internal sealed class TimestampInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        this.Apply(eventData);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        this.Apply(eventData);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContextEventData eventData)
    {
        if (eventData.Context is not { } context)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<IEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IEntity.Created)).CurrentValue = now;
                entry.Property(nameof(IEntity.Updated)).CurrentValue = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IEntity.Updated)).CurrentValue = now;
            }
        }
    }
}
