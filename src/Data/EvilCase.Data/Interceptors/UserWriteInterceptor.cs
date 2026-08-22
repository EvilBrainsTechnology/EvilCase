using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

/// <summary>
/// Fills a new row's <c>TenantId</c> and <c>UserId</c> from <see cref="IUserContext"/>. A row of another
/// tenant, or a row of another user within the same tenant, is refused on write, change or deletion.
/// </summary>
internal sealed class UserWriteInterceptor(IUserContext userContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        this.Apply(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        this.Apply(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        var entries = context?.ChangeTracker.Entries()
            .Where(entry => entry.Entity is ITenantEntity)
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        // The context is read only where the write touches a tenant row, so signing in still writes.
        if (entries is not { Count: > 0 })
            return;

        var tenantId = userContext.TenantId;

        foreach (var entry in entries)
        {
            Stamp(entry, nameof(ITenantEntity.TenantId), tenantId, "tenant");

            if (entry.Entity is IUserOwnedEntity)
                Stamp(entry, nameof(IUserOwnedEntity.UserId), userContext.UserId, "user");
        }
    }

    private static void Stamp(EntityEntry entry, string propertyName, in Guid id, string owner)
    {
        var property = entry.Property(propertyName);

        if (entry.State == EntityState.Added && (Guid)property.CurrentValue! == Guid.Empty)
        {
            property.CurrentValue = id;
            return;
        }

        if ((Guid)property.CurrentValue! != id)
        {
            throw new InvalidOperationException(
                $"{entry.Metadata.DisplayName()} is written under {owner} {id} but carries {property.CurrentValue}");
        }
    }
}
