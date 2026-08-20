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
        if (eventData.Context is { } context)
            EntityTimestamps.Apply(context.ChangeTracker, timeProvider.GetUtcNow().UtcDateTime);
    }
}
